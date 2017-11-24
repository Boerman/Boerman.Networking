using System;

namespace Boerman.Networking
{
    public static class Common
    {
		public static void InvokeEvent<T>(object source, EventHandler<T> eventHandler, T t) {
			try {
				eventHandler?.Invoke(source, t);
			} catch { }
		}

        public static void InvokeEvent<T>(EventHandler<T> eventHandler, T t)
        {
            try
            {
                eventHandler?.Invoke(null, t);
            } catch { }
        }
    }
}
