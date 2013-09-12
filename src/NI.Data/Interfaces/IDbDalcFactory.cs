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
using System.Data;
using System.Data.Common;

namespace NI.Data
{

	/// <summary>
	/// Database command wrapper factory interface
	/// </summary>
	public interface IDbDalcFactory {

		/// <summary>
		/// Create data adapter and bind row updating/updated event handlers
		/// </summary>
		IDbDataAdapter CreateDataAdapter(
			EventHandler<RowUpdatingEventArgs> onRowUpdating,
			EventHandler<RowUpdatedEventArgs> onRowUpdated);

		/// <summary>
		/// Create command 
		/// </summary>
		IDbCommand CreateCommand();

		/// <summary>
		/// Create connection 
		/// </summary>
		IDbConnection CreateConnection();

		/// <summary>
		/// Add new constant parameter
		/// </summary>
		string AddCommandParameter(IDbCommand cmd, object value);

		/// <summary>
		/// Add new data column parameter
		/// </summary>
		string AddCommandParameter(IDbCommand cmd, DataColumn column, DataRowVersion sourceVersion);

		/// <summary>
		/// Creare SQL builder
		/// </summary>
		IDbSqlBuilder CreateSqlBuilder(IDbCommand dbCommand);

		/// <summary>
		/// Get last insert ID
		/// </summary>
		object GetInsertId(IDbConnection connection);
	}

}
