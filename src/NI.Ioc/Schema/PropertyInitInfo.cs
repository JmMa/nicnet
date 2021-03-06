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

namespace NI.Ioc
{
	/// <summary>
	/// Property initialization information 
	/// </summary>
	public class PropertyInfo : IPropertyInitInfo
	{
		string _Name;
		IValueInitInfo _Value;
	
		public string Name { get { return _Name; } }
		
		public IValueInitInfo Value { get { return _Value; } }
	
		public PropertyInfo(string name, IValueInitInfo value)
		{
			_Name = name;
			_Value = value;
		}
		
	}
}
