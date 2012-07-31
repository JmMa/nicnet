#region License
/*
 * Open NIC.NET library (http://nicnet.googlecode.com/)
 * Copyright 2004-2012 NewtonIdeas
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
using System.Collections;

namespace NI.Common.Providers
{
	/// <summary>
	/// Generic object provider interface.
	/// </summary>
	public interface IObjectProvider
	{
		/// <summary>
		/// Returns object using context object
		/// </summary>
		/// <param name="context">can be null</param>
		/// <returns>object</returns>
		object GetObject(object context);
	}
}
