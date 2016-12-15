using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace BNAC
{
    class Util{
        public static string rsa_key =
        @"<RSAKeyValue><Modulus>xxxxxx</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public static string aes_key = "xxxxxx";
        public static int[,] xor_key = new int[,] { { 0, 0 }, { 1, 2 }, { 3, 5 }, { 9, 1 }, { 2, 7 }, { 1, 3 }, { 5, 6 }, { 7, 8 }, { 8, 9 }, { 3, 7 }, { 4, 6 } };


        //xor ok
        public static string xor(string d,int k){
            StringBuilder sb = new StringBuilder();
            int a=xor_key[k,0];
            int b=xor_key[k,1];
            for (int i = 0; i < d.Length; i++) {
                int c = (a + b) % 0xff;
                sb.Append( (char)(d[i] ^ c));
                Console.WriteLine("number is " + (d[i] ^ c));
                a = b;
                b = c;
            }
            return sb.ToString() ;
        }

        //获取uuid
        public static string getUuid() {
            return Guid.NewGuid().ToString("B").ToUpper();
        }

        //rsa encrypt
        public static string rsaEncrypt(string read_pass)
        {
            //111111
            byte[] data = Encoding.Default.GetBytes(rsa_key);
            byte[] pass_data = Encoding.Default.GetBytes(read_pass);
            RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider();
            //rsaKey.ImportCspBlob(data);
            rsaKey.FromXmlString(rsa_key);
            RSAPKCS1KeyExchangeFormatter pkcs = new RSAPKCS1KeyExchangeFormatter(rsaKey);

            byte[] out_data = pkcs.CreateKeyExchange(pass_data);

            Console.WriteLine(rsa_key);
            string out_string = BitConverter.ToString(out_data).Replace("-", "");
            Console.WriteLine("1st pass is :" + out_string);
            Console.WriteLine("sys pass is :" + Util.pass);

            //22222222222
            byte[] PlainTextBArray;
            byte[] CypherTextBArray;
            string Result;
            System.Security.Cryptography.RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(rsa_key);
            PlainTextBArray = (new UnicodeEncoding()).GetBytes(read_pass);
            CypherTextBArray = rsa.Encrypt(PlainTextBArray, false);
            Result = BitConverter.ToString(CypherTextBArray).Replace("-", "");//;Convert.ToBase64String(CypherTextBArray);
            Console.WriteLine("2nd pass is :" + Result);

            //3333333333333


            return Util.pass;

        }

        //time??
        public static string getTime(string session_id,string sockname){
            string bigstr = "xxxx:" + session_id + ":" + sockname;
            byte[] result = Encoding.Default.GetBytes(bigstr.Trim());
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(result);
            return BitConverter.ToString(output).Replace("-", "").ToLower();
        }

        
        public static string pass = "xxx";
        public static string pas2 = "xxx";


    }
}
