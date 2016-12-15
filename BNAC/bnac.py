#!/usr/bin/env python
import os, sys, socket, re, struct, time, getpass, uuid, hashlib
from Crypto.PublicKey import RSA
from Crypto.Cipher import PKCS1_v1_5
from Crypto.Cipher import AES

host, port = '172.22.1.144', 10001
bufsize = 8192
rsa_key = '''-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEArM43Q1ctTQ8pHp5dW8xk
Fm5hieEzm92MBx6M1uVf8Va3Qrt5rLcXK+YbFUyN/oAFB5hopx0QbWOM2hiohvxp
I+HB6rh5p/Q/Ywmm1tA3T/GdvttzFjhAyDnnTiY/O61m+hoEivavDcxLtkZ4dNy/
n1feI7zDc61LP40S+AG5+Qby6HyNetkWC8h01FwW8Hm3CY6vfEDJ3HPsqDKMnUaX
/PqoKv8f2sUFl/mcQz18LH0JNwND4qNUqI+BqpNKJsutpkOB6dGA9dXQtTGc2bzo
5IPxGsSrxJS01TSjqPoASoRj8YKVISJHHwkVbun+r5wx5OLtEFcMxxh3LELgIWDk
aQIDAQAB
-----END PUBLIC KEY-----'''
aes_key = '\x16\x25\x3A\x48\x55\x69\x77\x8C\x94\xA7\xBE\xC1\xD4\xE2\xFD\x11'
xor_key = ((0, 0), (1, 2), (3, 5), (9, 1), (2, 7), (1, 3), (5, 6), (7, 8), (8, 9), (3, 7), (4, 6))

rsa = PKCS1_v1_5.new(RSA.importKey(rsa_key))
aes = AES.new(aes_key)

def xor(d, k):
    r = ''
    a, b = xor_key[k]
    for i in d:
        c = (a + b) % 0xFF
        r += chr(ord(i) ^ c)
        a = b
        b = c
    return r

def encrypt(d, k):
    h = struct.pack('<i', len(d))
    d = d.ljust((len(d) + 15) / 16 * 16, '\0')
    d = xor(d, k)
    d = aes.encrypt(d)
    return h + d

def decrypt(d, k):
    l, d = struct.unpack('<i', d[:4])[0], d[4:]
    d = aes.decrypt(d)[:l]
    return xor(d, k)

def _encrypt(d,k):
    print d
    return d

def _decrypt(d,k):
    print d
    return d

#if len(sys.argv) == 2:
#    username = sys.argv[1]
#    password = getpass.getpass()
#elif len(sys.argv) == 3:
#    username, password = sys.argv[1:]
#else:
#    print 'Usage: %s username [password]' % sys.argv[0]
#    sys.exit()

username = raw_input("Please input your username: ")
password = raw_input("Please input your password: ")

sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.connect((host, port))

sock.send('ASK_ENCODE\r\nPLATFORM:MAC\r\nVERSION:1.0.1.22\r\nCLIENTID:BNAC_{%s}\r\n\r\n' % str(uuid.uuid4()).upper())
buf = sock.recv(bufsize)
mo_num = re.search('CIPHERNUM:(\d+)', buf)
if not (buf.startswith('601') and mo_num):
    print 'error ASK_ENCODE'
    sys.exit()
xor_num = int(mo_num.group(1))

sock.send(_encrypt('OPEN_SESAME\r\nSESAME_MD5:INVALID MD5\r\n\r\n', xor_num))
buf = _decrypt(sock.recv(bufsize), xor_num)
if not (buf.startswith('603')):
    print 'error OPEN_SESAME'
    sys.exit()

sock.send(_encrypt('SESAME_VALUE\r\nVALUE:0\r\n\r\n', xor_num))
buf = _decrypt(sock.recv(bufsize), xor_num)
if not (buf.startswith('604')):
    print 'error SESAME_VALUE'
    sys.exit()

sock.send(_encrypt('AUTH\r\nOS:MAC\r\nUSER:%s\r\nPASS:%s\r\nAUTH_TYPE:DOMAIN\r\n\r\n' % (username, rsa.encrypt(password).encode('hex')), xor_num))
buf = _decrypt(sock.recv(bufsize), xor_num)
mo_session_id = re.search('SESSION_ID:(\d+)', buf)
mo_role = re.search('ROLE:(\d+)', buf)
if not (buf.startswith('288') and mo_session_id and mo_role):
    print 'error AUTH'
    sys.exit()
session_id = mo_session_id.group(1)
role = mo_role.group(1)

sock.send(_encrypt('PUSH\r\nTIME:%s\r\nSESSIONID:%s\r\nROLE:%s\r\n\r\n' % (hashlib.md5('liuyan:%s:%s' % (session_id, sock.getsockname()[0])).hexdigest(), session_id, role), xor_num))
buf = _decrypt(sock.recv(bufsize), xor_num)
if not (buf.startswith('220')):
    print 'error PUSH'
    sys.exit()

print 'Passed Zhunru!'

heartbeat = 1
while True:
    time.sleep(60)
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.connect((host, port))
    sock.send(_encrypt('KEEP_ALIVE\r\nSESSIONID:%s\r\nUSER:%s\r\nAUTH_TYPE:DOMAIN\r\nHEARTBEAT_INDEX:%d\r\n\r\n' % (session_id, username, heartbeat), xor_num))
    heartbeat += 1
    sys.stdout.write('.')
    sys.stdout.flush()
