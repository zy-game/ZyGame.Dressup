using System.Collections.Generic;
using System;
using UnityEngine;

namespace ZyGame.Dressup
{
    /// <summary>
    /// 部位枚举
    /// </summary>
    public enum Element
    {
        None = 0,

        //===============头部============
        /// <summary>
        /// 头部
        /// </summary>
        Head = 19,
        /// <summary>
        /// 腮红
        /// </summary>
        Blush = 1,
        /// <summary>
        /// 眼珠
        /// </summary>
        Pupli = 2,
        /// <summary>
        /// 雀斑
        /// </summary>
        Freckles = 3,
        //===============头部============
        Eyebrow = 20,
        /// <summary>
        /// 额头
        /// </summary>
        Forehead = 7,
        /// <summary>
        /// 头发
        /// </summary>
        Hair = 5,
        /// <summary>
        /// 胡子
        /// </summary>
        Beard = 6,
        /// <summary>
        /// 眼睛配件
        /// </summary>
        EyeAccessory = 4,
        /// <summary>
        /// 头部配件
        /// </summary>
        HeadAccessory = 8,
        /// <summary>
        /// 纹身
        /// </summary>
        Tattoos = 9,
        /// <summary>
        /// 颈部
        /// </summary>
        Neck = 10,
        /// <summary>
        /// 身体
        /// </summary>
        Body = 11,
        /// <summary>
        /// 外套
        /// </summary>
        Coat = 12,
        /// <summary>
        /// 右手
        /// </summary>
        Handheld_R = 13,
        /// <summary>
        /// 翅膀
        /// </summary>
        Wings = 14,
        /// <summary>
        /// 尾巴
        /// </summary>
        Tail = 15,
        /// <summary>
        /// 袜子/丝袜
        /// </summary>
        Socks = 16,
        /// <summary>
        /// 鞋子
        /// </summary>
        Shoes = 17,
        /// <summary>
        /// 腿
        /// </summary>
        Legs = 18,
    }

    [Serializable]
    public class NodeData
    {
        public Element basic;
        public string path;
        public string group;
        public List<NodeChild> childs;
    }

    [Serializable]
    public class NodeChild
    {
        public Element element;
        public List<string> path;
    }

    [Serializable]
    public class ElementGroupData
    {
        public string name;
        public string skelton;
    }

    public class InitConfig
    {
        public List<NodeData> nodes;
        public List<Element> normal;
        public List<ElementGroupData> groups;
    }

    /// <summary>
    /// Avatar数据
    /// </summary>
    public sealed class DressupConfig
    {
        public string name;
        public string group;
        public string icon;
        public string md5;
        public List<DressupData> items;

        public DressupConfig()
        {
            items = new List<DressupData>();
        }

        public void Dispose()
        {
            Clear();
        }

        public void AddConfig(DressupData config)
        {
            this.items.Add(config);
        }

        public void RemoveConfig(DressupData config)
        {
            this.items.Remove(config);
        }

        public void Clear()
        {
            items.Clear();
        }
    }

    /// <summary>
    /// 部位数据
    /// </summary>
    public sealed class DressupData
    {
        /// <summary>
        /// 使用部件ID
        /// </summary>
        public string id;

        /// <summary>
        /// 部件使用的模型资源
        /// </summary>
        public string model;

        /// <summary>
        /// 部位
        /// </summary>
        public Element element;

        /// <summary>
        /// 部位使用的贴图
        /// </summary>
        public string texture;

        /// <summary>
        /// 部位Icon
        /// </summary>
        public string icon;

        /// <summary>
        /// 部位名称
        /// </summary>
        public string name;

        /// <summary>
        /// 分组名
        /// </summary>
        public string model_name;

        /// <summary>
        /// 发布状态
        /// </summary>
        public int publish_status;

        /// <summary>
        /// 一个整数版本号，增加这个数字可以迫使Unity重新下载缓存的资产捆绑包
        /// </summary>
        public uint version;

        /// <summary>
        /// 如果非零，则此数字将与下载资产的校验和进行比较 捆绑数据。如果CRC不匹配，将记录一个错误，并且资产 将不会加载捆绑包。如果设置为零，将跳过CRC检查。
        /// </summary>
        public uint crc;
    }

    public sealed class EventNames
    {
        /// <summary>
        /// 初始化完成事件
        /// </summary>
        public const string INITIALIZED_COMPLATED_EVENT = "INITIALIZED_COMPLATED_EVENT";

        /// <summary>
        /// 设置部件完成事件
        /// </summary>
        public const string SET_ELEMENT_DATA_COMPLATED = "SET_ELEMENT_DATA_COMPLATED";

        /// <summary>
        /// 导出配置完成事件
        /// </summary>
        public const string EXPORT_AVATAR_CONFIG_COMPLATED = "EXPORT_AVATAR_CONFIG_COMPLATED";

        /// <summary>
        /// 获取部件数据事件
        /// </summary>
        public const string GET_ELEMENT_DATA = "GET_ELEMENT_DATA";

        /// <summary>
        /// 合并Avatar事件
        /// </summary>
        public const string COMBINE_AVATAR_COMPLATED = "COMBINE_AVATAR_COMPLATED";

        /// <summary>
        /// 预览完成事件
        /// </summary>
        public const string PREVIEW_COMPLATED = "PREVIEW_COMPLATED";

        /// <summary>
        /// 清理部件事件
        /// </summary>
        public const string CLEAR_ELMENT_DATA_COMPLATED = "CLEAR_ELMENT_DATA_COMPLATED";

        /// <summary>
        /// 导入配置完成事件
        /// </summary>
        public const string IMPORT_CONFIG_COMPLATED = "IMPORT_CONFIG_COMPLATED";

        /// <summary>
        /// 下载失败事件
        /// </summary>
        public const string DOWNLOAD_ASSET_FAILUR = "DOWNLOAD_ASSET_FAILUR";

        /// <summary>
        /// 开始下载事件
        /// </summary>
        public const string DOWNLOAD_ASSET_START = "DOWNLOAD_ASSET_START";

        /// <summary>
        /// 下载成功事件
        /// </summary>
        public const string DOWNLOAD_ASSET_SUCCESS = "DOWNLOAD_ASSET_SUCCESS";

        /// <summary>
        /// 文件打开完成事件
        /// </summary>
        public const string OPEN_FILE_COMPLATED = "OPEN_FILE_COMPLATED";

        /// <summary>
        /// 部件资源上传成功
        /// </summary>
        public const string UPLOAD_ELEMENT_ASSET_COMPLATED = "UPLOAD_ELEMENT_ASSET_COMPLATED";

        /// <summary>
        /// 部件资源上传成功
        /// </summary>
        public const string UPLOAD_ELEMENT_ASSET_FAIL = "UPLOAD_ELEMENT_ASSET_FAIL";

        /// <summary>
        /// 错误通知
        /// </summary>
        public const string ERROR_MESSAGE_NOTICE = "ERROR_MESSAGE_NOTICE";

        /// <summary>
        /// 创建图层
        /// </summary>
        public const string CREATE_LAYER_COMPLETED = "CREATE_LAYER_COMPLETED";

        /// <summary>
        /// 开始涂鸦
        /// </summary>
        public const string GRAFFITI_INITIALIZED_COMPLETED = "GRAFFITI_INITIALIZED_COMPLETED";

        /// <summary>
        /// 提示是否保存涂鸦数据
        /// </summary>
        public const string NOTICE_SAVED_GRAFFITI_DATA = "NOTICE_SAVED_GRAFFITI_DATA";

        /// <summary>
        /// 导入涂鸦图片成功
        /// </summary>
        public const string IMPORT_GRAFFITI_TEXTURE_COMPLETED = "IMPORT_GRAFFITI_TEXTURE_COMPLETED";

        /// <summary>
        /// 导入涂鸦数据完成
        /// </summary>
        public const string IMPORT_GRAFFITI_DATA_COMPLETED = "IMPORT_GRAFFITI_DATA_COMPLETED";

        /// <summary>
        /// 保存涂鸦数据完成
        /// </summary>
        public const string SAVED_GRAFFITI_DATA_COMPLETED = "SAVED_GRAFFITI_DATA_COMPLETED";

        /// <summary>
        /// 删除图层完成
        /// </summary>
        public const string DELETE_LAYER_COMPLETED = "DELETE_LAYER_COMPLETED";

        /// <summary>
        /// 发布涂鸦完成
        /// </summary>
        public const string PUBLISHING_GRAFFITI_COMPLETED = "PUBLISHING_GRAFFITI_COMPLETED";

        /// <summary>
        /// 发布涂鸦完成
        /// </summary>
        public const string PUBLISHING_GRAFFITI_FAIL = "PUBLISHING_GRAFFITI_FAIL";

        /// <summary>
        /// 图层排序成功
        /// </summary>
        public const string SORT_LAYER_SUCCESSFLY = "SORT_LAYER_SUCCESSFLY";
    }

    public sealed class ErrorInfo
    {
        public const string INITIALIZE_AVATAR_ERROR_NOT_FIND_THE_SKELETON = "initializ the avatar fail,the skeleton is not find";
        public const string SHOW_IN_CAMERA_CENTER_FAIL = "show in center fail";
        public const string CLEAR_ELEMENT_FAIL = "clear element fail";
        public const string CONFIG_DATA_IS_NULL_OR_EMPTY = "the import config data is null or empty";
        public const string ELEMENT_GROUP_IS_NULL_OR_EMPTY = "Element Group Cannot be Null or Empty";
        public const string ELEMENT_GROUP_NOT_THE_SAME = "Element Group is not the same current group:{0} target group:{1}";
        public const string NOT_FIND_THE_ELEMENT_DATA = "Not find the element data";
        public const string LAYER_OUT_LIMIT_COUNT = "To reach the maximum number of floors";
        public const string NOT_FIND_TATH_LAYER = "Not find the layer name:{0}";
        public const string NOT_INITIALIZED_PAINT_DATA = "not initialized the paint data";
        public const string INITIALIZED_GRAFFITI_FAIL = "Initialized the graffiti fail:{0}";
        public const string NOT_INITIALIZED_AVATAR = "Please Initialized the avatar";
        public const string NOT_INITIALIZED_DRAWING = "Please Initialized the drawing";
        public const string NOT_FIND_THE_ELEMENT_POINT = "Not find the element point";
        public const string NOT_FIND_THE_ELEMENT_ASSET = "Not find the asset:{0}";
        public const string UPLOAD_AVATAR_ICON_FAIL = "upload avatar icon fail";
        public const string THE_ELEMENT_DATA_CAN_NOT_BE_NULL_OR_EMPTY = "can't be set the null or empty element data";
        public const string LOAD_FILE_FAIL = "load file fail:{0}";
        public const string IMPORT_AVATAR_CONFIG_FAIL = "import avatar config fail:{0}";
        public const string Please_set_the_model_first_before_setting_the_map = "Please set the model first before setting the map{0}";
    }
}