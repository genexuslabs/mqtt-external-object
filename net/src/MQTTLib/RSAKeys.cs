using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace MQTTLib
{
	/// <summary>
	/// Taken from https://gist.github.com/therightstuff/aa65356e95f8d0aae888e9f61aa29414
	/// </summary>
	public class RSAKeys
	{
		/// <summary>
		/// Import OpenSSH PEM private key string into MS RSACryptoServiceProvider
		/// </summary>
		/// <param name="pem"></param>
		/// <returns></returns>
		public static RSACryptoServiceProvider ImportPrivateKey(string pem, IPasswordFinder passwordFinder)
		{
			PemReader pr = new PemReader(new StringReader(pem), passwordFinder);
			AsymmetricCipherKeyPair KeyPair = (AsymmetricCipherKeyPair)pr.ReadObject();
			RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)KeyPair.Private);

			RSACryptoServiceProvider csp = new RSACryptoServiceProvider();// cspParams);
			csp.ImportParameters(rsaParams);
			return csp;
		}

		public static string GetPlainBase64(string pem)
		{
			if (pem.Contains("-----BEGIN"))
			{
				Regex rex = new Regex(@"-----.+-----");
				return rex.Replace(pem, "");
			}

			return pem;
		}
	}


	public class PasswordFinder : IPasswordFinder
	{
		char[] m_password;
		public PasswordFinder(string password)
		{
			m_password = password.ToCharArray();
		}

		public char[] GetPassword()
		{
			return m_password;
		}
	}
}
