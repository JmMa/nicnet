#region License
/*
 * Open NIC.NET library (http://nicnet.googlecode.com/)
 * Copyright 2004-2012 NewtonIdeas,  Vitalii Fedorchenko (v.2 changes)
 * Distributed under the LGPL licence
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;
using System.Xml;
using System.IO;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Configuration;
using System.Reflection;
using System.ComponentModel;
using System.Text;


namespace NI.Ioc
{
	
	/// <summary>
	/// Configuration section handler for XmlComponentConfiguration
	/// (so you may place configuration in the app.config or web.config)
	/// </summary>
	/// <example><code>
	/// &lt;configSections&gt;
	///		&lt;section name="ioc" type="NI.Ioc.XmlComponentConfigurationSectionHandler, NI.Ioc" /&gt;
	///	&lt;/configSections&gt;
	///	&lt;ioc&gt;
	///		&lt;!-- components definitions --&gt;
	///	&lt;/ioc&gt;
	/// </code></example>
	public class XmlComponentConfigurationSectionHandler : IConfigurationSectionHandler {

		public XmlComponentConfigurationSectionHandler() {
		}
		
		public virtual object Create(object parent, object input, XmlNode section) {
			try {
				var config = new XmlComponentConfiguration(section.InnerXml); 
				return config;
			} catch (Exception ex) {
				throw new ConfigurationException( ex.Message, ex);
			}
			
		}
		
	}


}
