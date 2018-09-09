using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Trubkin.Util
{
	// Установить ExecutionOrder раньше всех

	// 2 способа установить параметры: через методы класса в редакторе и через аргументы исполняемого файла
	// аргументы исполняемого файла имеют больший приоритет
	public class ArgumentHandler : MonoBehaviour, IConfig
	{
		private const char Divider = '='; 
		// Костыль! Unity не умеет сериализовать словари, а значит и запоминать их от редактора до старта
		// Значит используем лист строчек ключ=значение
		[SerializeField] private List<string> configs = new List<string>();

		public static ArgumentHandler Singleton { get; private set; }

		private ArgumentHandler()
		{
			if (Singleton != null)
			{
				Debug.LogWarning("Argument handler must be the only one.");
				return;
				;
			}

			Singleton = this;
		}

		private void Awake()
		{
			ParseApplicationArguments();
		}

		private void ParseApplicationArguments()
		{
			for (var i = 1; i < Environment.GetCommandLineArgs().Length; i++)
			{
				var arg = Environment.GetCommandLineArgs()[i];
				var config = arg.Split(Divider);
				if (config.Length >= 2)
				{
					SetValue(config[0], config[1]);
				}
				else
				{
					SetKey(config[0]);
				}
			}
		}
		
		public bool ContainsKey(string key)
		{
			return configs.Find(x => x.Split(Divider)[0] == key) != null;
		}

		public void SetKey(string key)
		{
			SetValue(key, null);
		}

		public string GetValue(string key)
		{
			return configs.Find(x => x.Split(Divider)[0] == key).Split(Divider)[1];
		}

		public void SetValue(string key, string value)
		{
			for (var i = 0; i < configs.Count; i++)
			{
				var config = configs[i];
				if (config.Split(Divider)[0] != key) continue;
				configs[i] = value == null ? key : key + Divider + value;
				return;
			}

			configs.Add(value == null ? key : key + Divider + value);
		}

		public bool Remove(string key)
		{
			string toRemove = null;
			foreach (var config in configs)
			{
				if (config.Split(Divider)[0] == key)
				{
					toRemove = config;
				}
			}

			return toRemove != null && configs.Remove(toRemove);
		}
	}
}