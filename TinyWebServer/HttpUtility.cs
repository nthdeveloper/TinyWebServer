using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace System.Web
{
	public sealed class HttpUtility
	{
		public static string UrlDecode(string str)
		{
			return UrlDecode(str, Encoding.UTF8);
		}
		
		public static string UrlDecode(string source, Encoding encoding)
		{
			if (null == source)
				return null;
			
			if (source.IndexOf ('%') == -1 && source.IndexOf ('+') == -1)
				return source;
			
			if (encoding == null)
				encoding = Encoding.UTF8;
			
			long len = source.Length;
			var bytes = new List<byte> ();
			int xchar;
			char ch;
			
			for (int i = 0; i < len; i++) {
				ch = source[i];
				if (ch == '%' && i + 2 < len && source[i + 1] != '%') {
					if (source[i + 1] == 'u' && i + 5 < len) {
						// unicode hex sequence
						xchar = GetChar (source, i + 2, 4);
						if (xchar != -1) {
							WriteCharBytes (bytes, (char)xchar, encoding);
							i += 5;
						} else
							WriteCharBytes (bytes, '%', encoding);
					} else if ((xchar = GetChar (source, i + 1, 2)) != -1) {
						WriteCharBytes (bytes, (char)xchar, encoding);
						i += 2;
					} else {
						WriteCharBytes (bytes, '%', encoding);
					}
					continue;
				}
				
				if (ch == '+')
					WriteCharBytes (bytes, ' ', encoding);
				else
					WriteCharBytes (bytes, ch, encoding);
			}
			
			byte[] buf = bytes.ToArray ();
			bytes = null;
			return encoding.GetString (buf);
			
		}				
		
		static int GetChar (string str, int offset, int length)
		{
			int val = 0;
			int end = length + offset;
			for (int i = offset; i < end; i++) {
				char c = str[i];
				if (c > 127)
					return -1;
				
				int current = GetInt ((byte)c);
				if (current == -1)
					return -1;
				val = (val << 4) + current;
			}
			
			return val;
		}	
		
		static void WriteCharBytes (IList buf, char ch, Encoding e)
		{
			if (ch > 255) {
				foreach (byte b in e.GetBytes (new char[] { ch }))
					buf.Add (b);
			} else
				buf.Add ((byte)ch);
		}		
		
		static int GetInt (byte b)
		{
			char c = (char)b;
			if (c >= '0' && c <= '9')
				return c - '0';
			
			if (c >= 'a' && c <= 'f')
				return c - 'a' + 10;
			
			if (c >= 'A' && c <= 'F')
				return c - 'A' + 10;
			
			return -1;
		}
		
	}
}
