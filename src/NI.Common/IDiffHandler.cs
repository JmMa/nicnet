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

namespace NI.Common
{
	
	/// <summary>
	/// Interface for 'diff' actions handler
	/// </summary>
	public interface IDiffHandler {

		/// <summary>
		/// Compare two elements
		/// </summary>
		int Compare(object arg1, object arg2);
		
		/// <summary>
		/// Merge action for two elements
		/// </summary>
		void Merge(object source, object destination);

		/// <summary>
		/// Add action
		/// </summary>
		void Add(object arg);

		/// <summary>
		/// Remove action
		/// </summary>
		void Remove(object arg);
	
	}
	
}
