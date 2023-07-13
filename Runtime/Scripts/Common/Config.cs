using System.Collections.Generic;
using System;
using UnityEngine;

namespace ZyGame.Dressup
{
    /// <summary>
    /// ��λö��
    /// </summary>
    public enum Element
    {
        None = 0,

        //===============ͷ��============
        /// <summary>
        /// ͷ��
        /// </summary>
        Head = 19,
        /// <summary>
        /// ����
        /// </summary>
        Blush = 1,
        /// <summary>
        /// ����
        /// </summary>
        Pupli = 2,
        /// <summary>
        /// ȸ��
        /// </summary>
        Freckles = 3,
        //===============ͷ��============
        Eyebrow = 20,
        /// <summary>
        /// ��ͷ
        /// </summary>
        Forehead = 7,
        /// <summary>
        /// ͷ��
        /// </summary>
        Hair = 5,
        /// <summary>
        /// ����
        /// </summary>
        Beard = 6,
        /// <summary>
        /// �۾����
        /// </summary>
        EyeAccessory = 4,
        /// <summary>
        /// ͷ�����
        /// </summary>
        HeadAccessory = 8,
        /// <summary>
        /// ����
        /// </summary>
        Tattoos = 9,
        /// <summary>
        /// ����
        /// </summary>
        Neck = 10,
        /// <summary>
        /// ����
        /// </summary>
        Body = 11,
        /// <summary>
        /// ����
        /// </summary>
        Coat = 12,
        /// <summary>
        /// ����
        /// </summary>
        Handheld_R = 13,
        /// <summary>
        /// ���
        /// </summary>
        Wings = 14,
        /// <summary>
        /// β��
        /// </summary>
        Tail = 15,
        /// <summary>
        /// ����/˿��
        /// </summary>
        Socks = 16,
        /// <summary>
        /// Ь��
        /// </summary>
        Shoes = 17,
        /// <summary>
        /// ��
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
    /// Avatar����
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
    /// ��λ����
    /// </summary>
    public sealed class DressupData
    {
        /// <summary>
        /// ʹ�ò���ID
        /// </summary>
        public string id;

        /// <summary>
        /// ����ʹ�õ�ģ����Դ
        /// </summary>
        public string model;

        /// <summary>
        /// ��λ
        /// </summary>
        public Element element;

        /// <summary>
        /// ��λʹ�õ���ͼ
        /// </summary>
        public string texture;

        /// <summary>
        /// ��λIcon
        /// </summary>
        public string icon;

        /// <summary>
        /// ��λ����
        /// </summary>
        public string name;

        /// <summary>
        /// ������
        /// </summary>
        public string model_name;

        /// <summary>
        /// ����״̬
        /// </summary>
        public int publish_status;

        /// <summary>
        /// һ�������汾�ţ�����������ֿ�����ʹUnity�������ػ�����ʲ������
        /// </summary>
        public uint version;

        /// <summary>
        /// ������㣬������ֽ��������ʲ���У��ͽ��бȽ� �������ݡ����CRC��ƥ�䣬����¼һ�����󣬲����ʲ� �����������������������Ϊ�㣬������CRC��顣
        /// </summary>
        public uint crc;
    }

    public sealed class EventNames
    {
        /// <summary>
        /// ��ʼ������¼�
        /// </summary>
        public const string INITIALIZED_COMPLATED_EVENT = "INITIALIZED_COMPLATED_EVENT";

        /// <summary>
        /// ���ò�������¼�
        /// </summary>
        public const string SET_ELEMENT_DATA_COMPLATED = "SET_ELEMENT_DATA_COMPLATED";

        /// <summary>
        /// ������������¼�
        /// </summary>
        public const string EXPORT_AVATAR_CONFIG_COMPLATED = "EXPORT_AVATAR_CONFIG_COMPLATED";

        /// <summary>
        /// ��ȡ���������¼�
        /// </summary>
        public const string GET_ELEMENT_DATA = "GET_ELEMENT_DATA";

        /// <summary>
        /// �ϲ�Avatar�¼�
        /// </summary>
        public const string COMBINE_AVATAR_COMPLATED = "COMBINE_AVATAR_COMPLATED";

        /// <summary>
        /// Ԥ������¼�
        /// </summary>
        public const string PREVIEW_COMPLATED = "PREVIEW_COMPLATED";

        /// <summary>
        /// �������¼�
        /// </summary>
        public const string CLEAR_ELMENT_DATA_COMPLATED = "CLEAR_ELMENT_DATA_COMPLATED";

        /// <summary>
        /// ������������¼�
        /// </summary>
        public const string IMPORT_CONFIG_COMPLATED = "IMPORT_CONFIG_COMPLATED";

        /// <summary>
        /// ����ʧ���¼�
        /// </summary>
        public const string DOWNLOAD_ASSET_FAILUR = "DOWNLOAD_ASSET_FAILUR";

        /// <summary>
        /// ��ʼ�����¼�
        /// </summary>
        public const string DOWNLOAD_ASSET_START = "DOWNLOAD_ASSET_START";

        /// <summary>
        /// ���سɹ��¼�
        /// </summary>
        public const string DOWNLOAD_ASSET_SUCCESS = "DOWNLOAD_ASSET_SUCCESS";

        /// <summary>
        /// �ļ�������¼�
        /// </summary>
        public const string OPEN_FILE_COMPLATED = "OPEN_FILE_COMPLATED";

        /// <summary>
        /// ������Դ�ϴ��ɹ�
        /// </summary>
        public const string UPLOAD_ELEMENT_ASSET_COMPLATED = "UPLOAD_ELEMENT_ASSET_COMPLATED";

        /// <summary>
        /// ������Դ�ϴ��ɹ�
        /// </summary>
        public const string UPLOAD_ELEMENT_ASSET_FAIL = "UPLOAD_ELEMENT_ASSET_FAIL";

        /// <summary>
        /// ����֪ͨ
        /// </summary>
        public const string ERROR_MESSAGE_NOTICE = "ERROR_MESSAGE_NOTICE";

        /// <summary>
        /// ����ͼ��
        /// </summary>
        public const string CREATE_LAYER_COMPLETED = "CREATE_LAYER_COMPLETED";

        /// <summary>
        /// ��ʼͿѻ
        /// </summary>
        public const string GRAFFITI_INITIALIZED_COMPLETED = "GRAFFITI_INITIALIZED_COMPLETED";

        /// <summary>
        /// ��ʾ�Ƿ񱣴�Ϳѻ����
        /// </summary>
        public const string NOTICE_SAVED_GRAFFITI_DATA = "NOTICE_SAVED_GRAFFITI_DATA";

        /// <summary>
        /// ����ͿѻͼƬ�ɹ�
        /// </summary>
        public const string IMPORT_GRAFFITI_TEXTURE_COMPLETED = "IMPORT_GRAFFITI_TEXTURE_COMPLETED";

        /// <summary>
        /// ����Ϳѻ�������
        /// </summary>
        public const string IMPORT_GRAFFITI_DATA_COMPLETED = "IMPORT_GRAFFITI_DATA_COMPLETED";

        /// <summary>
        /// ����Ϳѻ�������
        /// </summary>
        public const string SAVED_GRAFFITI_DATA_COMPLETED = "SAVED_GRAFFITI_DATA_COMPLETED";

        /// <summary>
        /// ɾ��ͼ�����
        /// </summary>
        public const string DELETE_LAYER_COMPLETED = "DELETE_LAYER_COMPLETED";

        /// <summary>
        /// ����Ϳѻ���
        /// </summary>
        public const string PUBLISHING_GRAFFITI_COMPLETED = "PUBLISHING_GRAFFITI_COMPLETED";

        /// <summary>
        /// ����Ϳѻ���
        /// </summary>
        public const string PUBLISHING_GRAFFITI_FAIL = "PUBLISHING_GRAFFITI_FAIL";

        /// <summary>
        /// ͼ������ɹ�
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