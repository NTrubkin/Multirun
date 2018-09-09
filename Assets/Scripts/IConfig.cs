namespace Trubkin.Util
{
	interface IConfig
	{
		bool ContainsKey(string key);
		void SetKey(string key);
		string GetValue(string key);
		void SetValue(string key, string value);
		bool Remove(string key);
	}
}