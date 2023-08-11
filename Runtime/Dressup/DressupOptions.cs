using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZyGame.Dressup
{
    public class DressupOptions
    {
        /// <summary>
        /// 服务器地址
        /// </summary>
        public string address;

        /// <summary>
        /// 资源组
        /// </summary>
        public string group;

        /// <summary>
        /// 骨架版本
        /// </summary>
        public uint version;

        /// <summary>
        /// 初始骨架
        /// </summary>
        public string skeleton;

        /// <summary>
        /// 骨架crc
        /// </summary>
        public uint crc;

        /// <summary>
        /// UID
        /// </summary>
        public string userId;

        /// <summary>
        /// 模型质量
        /// </summary>
        public string quality;

        /// <summary>
        /// 项目ID
        /// </summary>
        public int pid;


        [NonSerialized] public Camera camera;
        [NonSerialized] public List<Element> normals;

        [NonSerialized] public Vector3 cameraPosition;

        // [NonSerialized] public List<NodeData> nodeList;
        [NonSerialized] public Action OpenFileCallback;
        [NonSerialized] public IAssetLoader assetLoader;
        [NonSerialized] public IEventNotify Notify;
        [NonSerialized] public List<GroupInfo> groupDatas;
    }
}