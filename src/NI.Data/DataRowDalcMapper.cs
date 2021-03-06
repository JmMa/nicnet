﻿#region License
/*
 * Open NIC.NET library (http://nicnet.googlecode.com/)
 * Copyright 2004-2012 NewtonIdeas
 * Copyright 2008-2013 Vitalii Fedorchenko (changes and v.2)
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
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace NI.Data {
	
	/// <summary>
	/// DALC-based DataRow mapper component that implements CRUD operations
	/// </summary>
	public class DataRowDalcMapper {

		/// <summary>
		/// Get or set DALC instance
		/// </summary>
		public IDalc Dalc { get; set; }

		/// <summary>
		/// Get or set function that creates DataSet for specified source name
		/// </summary>
		public Func<string,DataSet> CreateDataSet { get; set; }

		/// <summary>
		/// Initializes a new instance of DataRowDalcMapper without resolving its dependencies
		/// </summary>
		/// <remarks>
		/// IDalc and CreateDataSet dependencies should be injected before calling any method of DataRowMapper
		/// </remarks>
		public DataRowDalcMapper() {
		}

		/// <summary>
		/// Initializes a new instance of DataRowDalcMapper with IDalc and dataset factory components
		/// </summary>
		/// <param name="dalc">IDalc instance</param>
		/// <param name="dsPrv">data set factory</param>
		public DataRowDalcMapper(IDalc dalc, Func<string, DataSet> dsPrv) {
			Dalc = dalc;
			CreateDataSet = dsPrv;
		}

		/// <summary>
		/// Create new DataRow
		/// </summary>
		/// <param name="tableName">data table name</param>
		public DataRow Create(string tableName) {
			DataSet ds = CreateDataSet(tableName);
			return ds.Tables[tableName].NewRow();
		}

		protected object PrepareValue(object o) {
			return o == null ? DBNull.Value : o;
		}

		/// <summary>
		/// Create new DataRow with specified data and insert it immediately.
		/// </summary>
		/// <param name="tableName">data table name</param>
		/// <param name="data">column -> value data</param>
		/// <returns>DataRow</returns>
		public DataRow Insert(string tableName, IDictionary<string, object> data) {
			DataRow r = Create(tableName);
			foreach (KeyValuePair<string, object> entry in data)
				r[entry.Key] = PrepareValue(entry.Value);
			Update(r);
			return r;
		}

		/// <summary>
		/// Load DataRow from specifed data source by single-value primary key
		/// </summary>
		/// <param name="tableName">data table name</param>
		/// <param name="pk"></param>
		/// <returns>DataRow or null</returns>
		public DataRow Load(string tableName, object pk) {
			return Load(tableName, new object[]{pk});
		}

		/// <summary>
		/// Load DataRow from specifed data source by primary key
		/// </summary>
		/// <param name="tableName">data table name</param>
		/// <param name="pk">primary key values</param>
		/// <returns>DataRow or null</returns>
		public DataRow Load(string tableName, params object[] pk) {
			DataSet ds = CreateDataSet(tableName);
			if (ds == null)
				throw new Exception("Unknown source name");
			Query q = new Query(tableName, ComposePkCondition(ds.Tables[tableName], pk));
			Dalc.Load(q, ds);
			return ds.Tables[q.Table].Rows.Count > 0 ? ds.Tables[q.Table].Rows[0] : null;
		}

		/// <summary>
		/// Load DataRow by query
		/// </summary>
		/// <param name="q">query</param>
		/// <returns>DataRow or null if no records matched</returns>
		public DataRow Load(Query q) {
			QTable table = new QTable(q.Table);
			DataSet ds = CreateDataSet(table.Name);
			if (ds == null)
				ds = new DataSet();
			var tbl = Dalc.Load(q, ds);
			return tbl.Rows.Count > 0 ? tbl.Rows[0] : null;
		}

		/// <summary>
		/// Load all records by query
		/// </summary>
		/// <param name="q">query</param>
		/// <returns>DataTable filled with data that matched specified query</returns>
		public DataTable LoadAll(Query q) {
			QTable source = new QTable(q.Table);
			DataSet ds = CreateDataSet(source.Name);
			if (ds == null)
				ds = new DataSet();
			var tbl = Dalc.Load(q, ds);
			return tbl;
		}

		/// <summary>
		/// Delete record from data source by single-value primary key
		/// </summary>
		/// <param name="tableName">data table name</param>
		/// <param name="pk">primary key value</param>
		/// <returns>true if record deleted successfully</returns>
		public bool Delete(string tableName, object pk) {
			return Delete(tableName, new object[] { pk });
		}

		/// <summary>
		/// Delete record from data source by primary key values
		/// </summary>
		/// <param name="tableName">data table name</param>
		/// <param name="pk">primary key values</param>
		/// <returns>true if record deleted successfully</returns>
		public bool Delete(string tableName, params object[] pk) {
			DataRow r = Load(tableName, pk);
            if (r != null) {
                Delete(r);
				return true;
            }
			return false;
		}

		/// <summary>
		/// Delete all records matched by query
		/// </summary>
		/// <param name="q">query</param>
		/// <returns>number of deleted records</returns>
        public int Delete(Query q) {
            DataTable tbl = LoadAll(q);
			var initialRowsCount = tbl.Rows.Count;
			foreach (DataRow r in tbl.Rows)
				r.Delete();
			Update(tbl);
			return initialRowsCount - tbl.Rows.Count;
        }

		/// <summary>
		/// Delete data row
		/// </summary>
		/// <param name="r">DataRow to delete</param>
		public void Delete(DataRow r) {
			if (r.RowState != DataRowState.Deleted)
				r.Delete();
			Dalc.Update(r.Table);
		}

		/// <summary>
		/// Update data row in dats source
		/// </summary>
		/// <param name="r">DataRow to update</param>
		public void Update(DataRow r) {
			// this is good place to replace 'null' with DBNull
			if (r.RowState!=DataRowState.Deleted)
				foreach (DataColumn c in r.Table.Columns)
					if (c.AllowDBNull && r[c] == null)
						r[c] = DBNull.Value;

			if (r.RowState == DataRowState.Detached)
				r.Table.Rows.Add(r);
			Dalc.Update(r.Table);
		}

		/// <summary>
		/// Update all modified rows in DataTable
		/// </summary>
		/// <param name="tbl">DataTable with changed rows</param>
		public void Update(DataTable tbl) {
			Dalc.Update(tbl);
		}

		/// <summary>
		/// Update record in data source by single-value primary key
		/// </summary>
		/// <param name="tableName">data table name</param>
		/// <param name="pk">primary key value</param>
		/// <param name="changeset">column name -> value</param>
		public void Update(string tableName, object pk, IDictionary<string, object> changeset) {
			Update(tableName, new object[] { pk }, changeset);
		}

		/// <summary>
		/// Update record in data source by primary key
		/// </summary>
		/// <param name="tableName">data source identifier</param>
		/// <param name="pk">primary key values</param>
		/// <param name="changeset">column name -> value</param>
		public void Update(string tableName, object[] pk, IDictionary<string, object> changeset) {
			DataSet ds = CreateDataSet(tableName);
			if (ds == null)
				throw new Exception("Unknown source name");
			Query q = new Query(tableName, ComposePkCondition(ds.Tables[tableName], pk) );
			var t = Dalc.Load(q, ds);
			if (t.Rows.Count==0)
				throw new Exception("Record does not exist");
			foreach (DataRow r in t.Rows) {
				foreach (KeyValuePair<string, object> entry in changeset)
					r[entry.Key] = PrepareValue(entry.Value);
			}
			Dalc.Update(t);
		}

		/// <summary>
		/// Update records matched by query
		/// </summary>
		/// <param name="q">query for matching records to update</param>
		/// <param name="changeset">changeset data</param>
		/// <returns></returns>
		public int Update(Query q, IDictionary<string, object> changeset) {
			var tbl = LoadAll(q);
			foreach (DataRow r in tbl.Rows)
				foreach (var entry in changeset)
					if (tbl.Columns.Contains(entry.Key))
						r[entry.Key] = entry.Value;
			Update(tbl);
			return tbl.Rows.Count;
		}


		protected QueryNode ComposePkCondition(DataTable tbl, params object[] pk) {
			QueryGroupNode grp = new QueryGroupNode(QueryGroupNodeType.And);
			if (tbl.PrimaryKey.Length != pk.Length)
				throw new Exception("Invalid primary key");
			for (int i=0; i<tbl.PrimaryKey.Length; i++) {
				grp.Nodes.Add( new QueryConditionNode( (QField)tbl.PrimaryKey[i].ColumnName, Conditions.Equal, new QConst(pk[i]) ) );
			}
			return grp;
		}



	}


}
