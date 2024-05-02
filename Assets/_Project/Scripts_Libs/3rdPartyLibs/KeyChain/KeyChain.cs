using System.Runtime.InteropServices;

#if UNITY_IOS
namespace CookApps.iOS {
	public class KeyChain {
		[DllImport("__Internal")]
		private static extern string getKeyChainValue(string key);
		[DllImport("__Internal")]
		private static extern void setKeyChainValue(string key, string value);
		[DllImport("__Internal")]
		private static extern void deleteKeyChainValue(string key);

		public static string GetKeyChainValue(string key) {
			return getKeyChainValue(key);
		}

		public static void SetKeyChainValue(string key, string value) {
			setKeyChainValue(key, value);
		}

		public static void DeleteKeyChainValue(string key) {
			deleteKeyChainValue(key);
		}
	}
}
#endif
