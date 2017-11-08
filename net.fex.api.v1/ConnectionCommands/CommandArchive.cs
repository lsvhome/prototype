using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

using Newtonsoft.Json.Linq;

namespace Net.Fex.Api
{
    public class CommandArchive : CommandBaseAuthorizedUser
    {
        /*
request:

"offset: 0
limit: 10"

response:

{"count":0,"object_list":[],"result":1,"offset":0,"limit":1,"all_count":0}


"{
        ""limit"": 1, // значение лимита из запроса или стандартное
        ""offset"": 0, // значение оффсета из запроса или стандартное
        ""result"": 1,
        ""count"": 1, // количество отданных объектов
        ""object_list"": [{ // массив объектов пользтвателя
                ""upload_size"": 36082, // размер объекта
                ""pay"": 1, // помещен ли объект в платное хранилище (1/0)
                ""with_view_pass"": 0, // установлен ли пароль на просмотр
                ""preview"": """", // сокращенное описание
                ""modify_time"": 1498552159, // время последнего изменения
                ""upload_count"": 1, // кол-во загруженных файлов
                ""login"": ""dvzdvz"", // логин владельца
                ""token"": ""879713320553"", // ключ
                ""create_time"": 1498552159 // время создания
        }],
        ""all_count"": 86
}"


{"all_count":1,"result":1,"offset":0,"limit":1,"count":1,"object_list":[{"can_edit":1,"modify_time":1510145975,"upload_count":1,"view_pass_hint":"","upload_size":879,"token":"241109264790","public":1,"pay":1,"preview":"","create_time":1510145923,"with_view_pass":0,"login":"slutai"}]}
             */

        [DataContract]
        public class CommandArchiveResponse
        {
            /// <summary>
            /// значение лимита из запроса или стандартное
            /// </summary>
            [DataMember]
            public int Limit { get; set; }

            /// <summary>
            /// значение оффсета из запроса или стандартное
            /// </summary>
            [DataMember]
            public int Offset { get; set; }

            [DataMember]
            public int Result { get; set; }

            /// <summary>
            /// количество отданных объектов
            /// </summary>
            [DataMember]
            public int Count { get; set; }

            /// <summary>
            /// Количество объектов всего (all_count)
            /// </summary>
            [DataMember(Name = "all_count")]
            public int Total { get; set; }

            /// <summary>
            /// массив объектов пользтвателя
            /// </summary>
            [DataMember(Name = "object_list")]
            public CommandArchiveResponseObject[] ObjectList { get; set; }
        }

        [DataContract]
        public class CommandArchiveResponseObject
        {
            /// <summary>
            /// размер объекта
            /// </summary>
            [DataMember(Name = "upload_size")]
            public int UploadSize { get; set; }

            /// <summary>
            /// Помещен ли объект в платное хранилище (1/0)
            /// </summary>
            [DataMember]
            public int Pay { get; set; }

            /// <summary>
            /// Установлен ли пароль на просмотр (1/0)
            /// </summary>
            [DataMember(Name = "with_view_pass")]
            public int WithViewPass { get; set; }

            /// <summary>
            /// Сокращенное описание
            /// </summary>
            [DataMember]
            public string Preview { get; set; }

            /// <summary>
            /// время последнего изменения (1498552159)
            /// </summary>
            [DataMember(Name = "modify_time")]
            public int ModifyTime { get; set; }

            /// <summary>
            /// время создания
            /// </summary>
            [DataMember(Name = "create_time")]
            public int CreateTime { get; set; }

            /// <summary>
            /// кол-во загруженных файлов
            /// </summary>
            [DataMember(Name = "upload_count")]
            public int UploadCount { get; set; }

            /// <summary>
            /// логин владельца
            /// </summary>
            [DataMember]
            public string Login { get; set; }

            /// <summary>
            /// ключ
            /// </summary>
            [DataMember]
            public string Token { get; set; }
        }

        public CommandArchive(int offset, int limit) : base(
            new Dictionary<string, string>
            {
                { "offset", offset.ToString() },
                { "limit", limit.ToString() }
            })
        {
        }

        public CommandArchive(IDictionary<string, string> parameters) : base(parameters)
        {
        }

        protected override string Suffix => "j_archive";

        public CommandArchiveResponse Result
        {
            get
            {
                if (this.ResultJObject.Value<int>("result") == 1)
                {
                    return this.ResultJObject.ToObject<CommandArchive.CommandArchiveResponse>();
                }
                else
                {
                    throw new ConnectionException();
                }
            }
        }
    }
}
