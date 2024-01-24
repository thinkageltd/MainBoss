using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents the settings for Ajax requests.
    /// </summary>
    /// <remarks>
    /// More information: http://api.jquery.com/jQuery.ajax/
    /// </remarks>
    public class AjaxSettings
    {
        private Dictionary<string, object> m_Properties = new Dictionary<string, object>();

        /// <summary>
        /// Sets a value indicating whether the requests are sent asynchronously.
        /// </summary>
        public bool Async
        {
            set
            {
                m_Properties[AjaxSettingsPropertyName.Async] = value;
            }
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered before an Ajax request is sent.
        /// </summary>
        public string BeforeSend
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.BeforeSend] = value;
            }
        }

        /// <summary>
        /// Sets a value indicating whether the browser should cache the requested pages.
        /// </summary>
        public bool Cache
        {
            set
            {
                m_Properties[AjaxSettingsPropertyName.Cache] = value;
            }
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when an Ajax request is completed.
        /// </summary>
        public string Complete
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.Complete] = value;
            }
        }

        /// <summary>
        /// Sets the content type.
        /// </summary>
        public string ContentType
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.ContentType] = value;
            }
        }

        /// <summary>
        /// Sets a value indicating whether a cross domain request is forced on the same domain.
        /// </summary>
        public bool CrossDomain
        {
            set
            {
                m_Properties[AjaxSettingsPropertyName.CrossDomain] = value;
            }
        }

        /// <summary>
        /// Sets the data to send to the server.
        /// </summary>
        public object Data
        {
            set
            {
                Guard.ArgumentNotNull(() => value);
                m_Properties[AjaxSettingsPropertyName.Data] = value;
            }
        }

        /// <summary>
        /// Sets the name of the JavaScript function that handles the raw response data.
        /// </summary>
        public string DataFilter
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.DataFilter] = value;
            }
        }

        /// <summary>
        /// Sets the type of data expected back from the server.
        /// </summary>
        public string DataType
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.DataType] = value;
            }
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when an Ajax request is failed.
        /// </summary>
        public string Error
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.Error] = value;
            }
        }

        /// <summary>
        /// Sets a value indicating whether to trigger global Ajax event handlers.
        /// </summary>
        public bool Global
        {
            set
            {
                m_Properties[AjaxSettingsPropertyName.Global] = value;
            }
        }

        /// <summary>
        /// Sets a value indicating whether the request is allowed to be successful only if the response has changed since the last request. 
        /// </summary>
        public bool IfModified
        {
            set
            {
                m_Properties[AjaxSettingsPropertyName.IfModified] = value;
            }
        }

        /// <summary>
        /// Sets a value indicating whether the current environment is recognized as 'local'.
        /// </summary>
        public bool IsLocal
        {
            set
            {
                m_Properties[AjaxSettingsPropertyName.IsLocal] = value;
            }
        }

        /// <summary>
        /// Sets the callback function name in JSONP requests.
        /// </summary>
        public string Jsonp
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.Jsonp] = value;
            }
        }

        /// <summary>
        /// Sets the MIME type.
        /// </summary>
        public string MimeType
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.MimeType] = value;
            }
        }

        /// <summary>
        /// Sets the charset for requests with 'jsonp' or 'script' dataType.
        /// </summary>
        public string ScriptCharset
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.ScriptCharset] = value;
            }
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when an Ajax request is succeeded.
        /// </summary>
        public string Success
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.Success] = value;
            }
        }

        /// <summary>
        /// Sets the timeout for the request.
        /// </summary>
        public uint Timeout
        {
            set
            {
                m_Properties[AjaxSettingsPropertyName.Timeout] = value;
            }
        }

        /// <summary>
        /// Sets a value indicating whether the traditional style of parameter serialization is used. 
        /// </summary>        
        public bool Traditional
        {
            set
            {
                m_Properties[AjaxSettingsPropertyName.Traditional] = value;
            }
        }

        /// <summary>
        /// Sets the type of the request.
        /// </summary>
        [Obsolete("Use the AjaxSettings.Method property instead.")]
        public string Type
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.Type] = value;
            }
        }

        /// <summary>
        /// Sets the HTTP method.
        /// </summary>
        public HttpVerbs Method
        {
            set
            {                
                m_Properties[AjaxSettingsPropertyName.Type] = value.ToString().ToUpperInvariant();
            }
        }

        /// <summary>
        /// Sets the URL.
        /// </summary>
        public string Url
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.Url] = value;
            }
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is called to create the XMLHttpRequest object.
        /// </summary>
        public string Xhr
        {
            set
            {
                Guard.ArgumentNotNullOrWhiteSpace(() => value);
                m_Properties[AjaxSettingsPropertyName.Xhr] = value;
            }
        }

        /// <summary>
        /// Returns the JSON string representation of this settings.
        /// </summary>
        /// <returns>The JSON string representation of this settings.</returns>
        public string ToJsonString()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Serialize(m_Properties);
        }
    }
}
