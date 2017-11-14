using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mek.PasswordUtils
{
	public class PasswordData
	{
		public byte[] Hash { get; set; }
		public byte[] Salt { get; set; }
		public int IterationCount { get; set; }
	}
}
