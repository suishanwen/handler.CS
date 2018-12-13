using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
namespace controller.util
{
    /// <summary>
    /// 封装HTTP get post请求，简化发送http请求
    /// </summary>
    class HttpManager
    {
        private HttpManager() { }
        private static HttpManager instance = new HttpManager();
        public static HttpManager getInstance()
        {
            return instance;
        }

        /// <summary>
        /// 发送get请求得到响应内容
        /// </summary>
        /// <param name="url_prex">url前缀</param>
        /// <param name="url">请求路径url</param>
        /// <param name="param">请求参数键值对</param>
        /// <returns>响应字符串</returns>
        public String requestHttpGet(String url_prex, String url, String param)
        {
            String responseContent = "";
            HttpWebResponse httpWebResponse = null;
            Stream streamReader = null;
            try
            {
                url = url_prex + url;
                if (param != null && !param.Equals(""))
                {
                    url = url + "?" + param;
                }
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.Method = "GET";
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                streamReader = httpWebResponse.GetResponseStream();
                if (streamReader == null)
                {
                    return "";
                }
                byte[] buf = new byte[1024];
                while (true)
                {
                    int len = streamReader.Read(buf, 0, buf.Length);
                    if (len <= 0)
                        break;
                    responseContent += System.Text.Encoding.GetEncoding("gbk").GetString(buf, 0, len);

                }

                if (responseContent == null || "".Equals(responseContent))
                {
                    return "";
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (httpWebResponse != null)
                {
                    httpWebResponse.Close();

                }
                if (streamReader != null)
                {
                    streamReader.Close();
                }
            }
            return responseContent;
        }
        /// <summary>
        /// 发送post请求得到响应内容
        /// </summary>
        /// <param name="url_prex">url前缀</param>
        /// <param name="url">请求路径url</param>
        /// <param name="paras">请求数据键值对</param>
        /// <returns>响应字符串</returns>
        public String requestHttpPost(String url_prex, String url, Dictionary<String, String> paras)
        {
            String responseContent = "";
            HttpWebResponse httpWebResponse = null;
            StreamReader streamReader = null;
            try
            {  //组装访问路径
                url = url_prex + url;
                //根据url创建HttpWebRequest对象
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                //设置请求方式和头信息
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                //遍历参数集合
                if (!(paras == null || paras.Count == 0))
                {
                    StringBuilder buffer = new StringBuilder();
                    int i = 0;
                    foreach (string key in paras.Keys)
                    {
                        if (i > 0)
                        {
                            buffer.AppendFormat("&{0}={1}", key, paras[key]);
                        }
                        else
                        {
                            buffer.AppendFormat("{0}={1}", key, paras[key]);
                        }
                        i++;
                    }
                    byte[] btBodys = Encoding.UTF8.GetBytes(buffer.ToString());
                    httpWebRequest.ContentLength = btBodys.Length;
                    //将请求内容封装在请求体中
                    httpWebRequest.GetRequestStream().Write(btBodys, 0, btBodys.Length);
                }
                //获取响应
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                //得到响应流
                streamReader = new StreamReader(httpWebResponse.GetResponseStream());
                //读取响应内容
                responseContent = streamReader.ReadToEnd();
                //关闭资源
                httpWebResponse.Close();
                streamReader.Close();
                //返回结果
                if (responseContent == null || "".Equals(responseContent))
                {
                    return "";
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (httpWebResponse != null)
                {
                    httpWebResponse.Close();

                }
                if (streamReader != null)
                {
                    streamReader.Close();
                }
            }
            return responseContent;
        }

        /// <summary>
        /// Http下载文件
        /// </summary>
        public string HttpDownloadFile(string url, string path)
        {
            // 设置参数
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            //发送请求并获取相应回应数据
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            //直到request.GetResponse()程序才开始向目标网页发送Post请求
            Stream responseStream = response.GetResponseStream();
            //创建本地文件写入流
            Stream stream = new FileStream(path, FileMode.Create);
            byte[] bArr = new byte[1024];
            int size = responseStream.Read(bArr, 0, (int)bArr.Length);
            while (size > 0)
            {
                stream.Write(bArr, 0, size);
                size = responseStream.Read(bArr, 0, (int)bArr.Length);
            }
            stream.Close();
            responseStream.Close();
            return path;
        }

    }
}