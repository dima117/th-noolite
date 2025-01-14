﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using ThinkingHome.NooLite.Web.Configuration;
using ThinkingHome.NooLite.Web.Models;



namespace ThinkingHome.NooLite.Web.Controllers
{
	public class HomeController : Controller
	{
		#region debug

		private static readonly List<string> channelNames = new List<string>
			{
				"Канал освещения 0", 
				"Канал освещения 1", 
				"Канал освещения 2", 
				"Канал освещения 3",
				"Канал освещения 4",
				"Канал освещения 5",
				"Канал освещения 6",
				"Канал освещения 7"
			};

		#endregion

		private static readonly NooLiteConfigurationSection current =
			ConfigurationManager.GetSection("nooLiteConfiguration") as NooLiteConfigurationSection;

		/// <summary>
		/// Системные настройки (находятся в конфигурационном файле приложения)
		/// </summary>
		public static NooLiteConfigurationSection CurrentConfig
		{
			get { return current; }
		}

		public ActionResult Index()
		{
			var model = new List<ControlPageModel>();

			var title = CurrentConfig.Title;
			ViewBag.Title = string.IsNullOrWhiteSpace(title) ? "Комнаты" : title;

			foreach (ControlPageElement page in CurrentConfig.ControlPages)
			{
				var pageModel = new ControlPageModel
					{
						Id = page.Id,
						Title = page.Title,
						Description = page.Description
					};

				foreach (ControlElement control in page.Controls)
				{
					var controlModel = new ControlModel
					{
						Id = control.Id,
						DisplayText = control.DisplayText,
						Level = control.Level,
						TemplateName = control.Type.ToString()
					};

					pageModel.Controls.Add(controlModel);
				}

				model.Add(pageModel);
			}


			return View(model);
		}

		public ActionResult OptionsCommand(string commandName, byte channel)
		{
			var messages = new List<string>();
			try
			{
				PC11XXCommand? cmd = null;

				switch (commandName.ToLower())
				{
					case "bind":
						cmd = PC11XXCommand.Bind;
						break;
					case "unbind":
						cmd = PC11XXCommand.UnBind;
						break;
				}

				if (cmd.HasValue)
				{
					if (CurrentConfig.Debug)
					{
						string msg = string.Format("COMMAND: {0}, CHANNEL: {1}", cmd.Value.ToString().ToUpper(), channel);
						messages.Add(msg);
					}
					else
					{
						using (var adapter = new PC11XXAdapter())
						{
							if (adapter.OpenDevice())
							{
								adapter.SendCommand(cmd.Value, channel);
							}
							else
							{
								messages.Add("PC11xx adapter not found");
							}
						}
					}
				}
				else
				{
					string msg = string.Format("Unknown command: {0}", commandName);
					messages.Add(msg);
				}
			}
			catch (Exception ex)
			{
				messages.Add(ex.Message);
			}

			return Json(messages, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Command(string page, string control, byte level, bool strong)
		{
			var messages = new List<string>();

			try
			{
				var commands = GetCommandList(page, control, level, strong);

				if (CurrentConfig.Debug)
				{

					foreach (var cmd in commands)
					{
						string channelName = channelNames[cmd.Channel];
						string actionName = cmd.Command == PC11XXCommand.SetLevel
												? string.Format("SET LEVEL {0}", cmd.Level)
												: cmd.Command.ToString().ToUpper();

						string msg = string.Format("{0} (channel {1}): {2}", channelName, cmd.Channel, actionName);
						messages.Add(msg);
					}
				}
				else
				{
					using (var adapter = new PC11XXAdapter())
					{
						if (adapter.OpenDevice())
						{
							foreach (var cmd in commands)
							{
								adapter.SendCommand(cmd.Command, cmd.Channel, cmd.Level);
							}
						}
						else
						{
							messages.Add("PC11xx adapter not found");
						}
					}
				}
			}
			catch (Exception ex)
			{
				messages.Add(ex.Message);
			}


			return Json(messages, JsonRequestBehavior.AllowGet);
		}

		private static List<PC11XXCommandData> GetCommandList(string pageId, string controlId, byte level, bool strong)
		{
			var result = new List<PC11XXCommandData>();

			var page = CurrentConfig.ControlPages.GetPage(pageId);

			if (page != null)
			{
				var control = page.Controls.GetControl(controlId);

				if (control != null)
				{
					foreach (ChannelElement channel in control.Channels)
					{
						byte lvl = strong ? level : channel.Level.GetValueOrDefault(level);

						lvl = lvl < 100 ? lvl : (byte)100;

						var cmd = lvl == 0 ? PC11XXCommand.Off : PC11XXCommand.SetLevel;

						// преобразуем процентное значение яркости в уровень яркости для адаптера
						var x = cmd == PC11XXCommand.SetLevel ? 40 + lvl : 0;
						result.Add(new PC11XXCommandData { Channel = channel.Id, Command = cmd, Level = (byte)x });
					}
				}
			}

			return result;
		}
	}
}
