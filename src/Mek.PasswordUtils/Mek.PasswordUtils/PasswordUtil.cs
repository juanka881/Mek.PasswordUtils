using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mek.PasswordUtils
{
	public class PasswordUtil
	{
		public static readonly int DefaultSaltLength = 24;
		public static readonly int DefaultDerivedKeyLength = 32;
		public static readonly int DefaultIterationCount = 10000;

		public static PasswordData CreatePasswordData(string text, int derivedKeyLength, int saltLength, int iterationCount)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));

			var saltBytes = GetRandomSaltBytes(saltLength);			
			var passwordHashBytes = GetdPasswordHashBytes(text, derivedKeyLength, saltBytes, iterationCount);

			var password = new PasswordData
			{
				Hash = passwordHashBytes,
				Salt = saltBytes,
				IterationCount = iterationCount
			};

			return password;
		}

		public static string ToPasswordHashString(PasswordData data)
		{
			var hashString = Convert.ToBase64String(data.Hash);
			var saltString = Convert.ToBase64String(data.Salt);
			var result = $"{hashString}:{saltString}:{data.IterationCount}";

			return result;
		}

		public static PasswordData FromPasswordHashString(string passwordHashString)
		{
			var tokens = passwordHashString.Split(':');

			if(tokens.Length != 3)
				throw new Exception("invalid password hash string, expected 3 tokens");

			var hashString = tokens[0];
			var hash = Convert.FromBase64String(hashString);
			var saltString = tokens[1];
			var salt = Convert.FromBase64String(saltString);
			var iterationCount = int.Parse(tokens[2]);

			var data = new PasswordData
			{
				Hash = hash,
				Salt = salt,
				IterationCount = iterationCount
			};

			return data;
		}

		public static PasswordData CreatePasswordData(string text)
		{
			return CreatePasswordData(text, DefaultDerivedKeyLength, DefaultSaltLength, DefaultIterationCount);
		}

		public static string CreatePasswordHashString(string password)
		{
			if(string.IsNullOrWhiteSpace(password))
				throw new ArgumentException($"{password} is empty or null", nameof(password));

			var passwordData = CreatePasswordData(password, DefaultDerivedKeyLength, DefaultSaltLength, DefaultIterationCount);
			var passwordHash = ToPasswordHashString(passwordData);

			return passwordHash;
		}

		public static bool ValidatePassword(string passwordText, PasswordData password)
		{
			if (passwordText == null)
				throw new ArgumentNullException(nameof(passwordText));

			if (password == null)
				throw new ArgumentNullException(nameof(password));

			var passwordTextHash = GetdPasswordHashBytes(
				passwordText, 
				password.Hash.Length, 
				password.Salt, 
				password.IterationCount);

			var result = ConstantTimeCompare(password.Hash, passwordTextHash);

			return result;
		}

		public static bool ValidatePasswordHashString(string passwordText, string passwordHashString)
		{
			var passwordData = FromPasswordHashString(passwordHashString);
			return ValidatePassword(passwordText, passwordData);
		}

		public static byte[] GetdPasswordHashBytes(string passwordText, int derivedKeyLength, byte[] saltBytes, int iterationCount)
		{
			using (var pbkdf2 = new Rfc2898DeriveBytes(passwordText, saltBytes, iterationCount))
			{
				var passwordBytes = pbkdf2.GetBytes(derivedKeyLength);
				return passwordBytes;
			}
		}

		public static byte[] GetRandomSaltBytes(int saltLength)
		{
			using(var rng = new RNGCryptoServiceProvider())
			{
				var salt = new byte[saltLength];
				rng.GetBytes(salt);

				return salt;	
			}
		}

		public static bool ConstantTimeCompare(byte[] dataX, byte[] dataY)
		{
			if (dataX == null)
				throw new ArgumentNullException(nameof(dataX));

			if (dataY == null)
				throw new ArgumentNullException(nameof(dataY));

			var diff = (uint)dataY.Length ^ (uint)dataX.Length;

			for(var i = 0; i < dataY.Length && i < dataX.Length; i++)
			{
				diff = diff | (uint)(dataY[i] ^ dataX[i]);
			}

			return diff == 0;			
		}
	}
}
