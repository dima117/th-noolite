﻿using System.IO;
using System.Xml.Serialization;

namespace ThinkingHome.NooLite.Web.Configurator
{
	public static class Config
	{
		public static NooLiteConfiguration Load()
		{
			var mySerializer = new XmlSerializer(typeof(NooLiteConfiguration));

			using (var stream = new FileStream("NooLite.config", FileMode.Open))
			{
				return  mySerializer.Deserialize(stream) as NooLiteConfiguration;
			}
		}

		public static void SaveConfig(NooLiteConfiguration obj)
		{
			var ns = new XmlSerializerNamespaces();
			ns.Add("", "");

			var mySerializer = new XmlSerializer(typeof(NooLiteConfiguration));

			using (var stream = new FileStream("NooLite.config", FileMode.Create))
			{
				mySerializer.Serialize(stream, obj, ns);
			}
		}
	}
}
