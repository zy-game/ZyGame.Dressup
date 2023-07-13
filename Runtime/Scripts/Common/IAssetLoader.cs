using System;
using Object = UnityEngine.Object;

namespace ZyGame.Dressup
{
    public interface IAssetLoader : IDisposable
    {
        void LoadAsync<T>(string url, uint version, uint crc, Action<T> completion) where T : Object;
        void Release(Object obj);
    }
}