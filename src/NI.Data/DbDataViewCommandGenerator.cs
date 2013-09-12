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
using System.Data;

namespace NI.Data
{
	/// <summary>
	/// Database Command Generator with data view support.
	/// </summary>
	public class DbDataViewCommandGenerator : DbCommandGenerator
	{
		IDbDataView[] _DataViews = new IDbDataView[0];

		public IDbDataView[] DataViews {
			get { return _DataViews; }
			set { _DataViews = value; }
		}
	
	
		public DbDataViewCommandGenerator(IDbDalcFactory factory) : base(factory)
		{
		}
		
		public override IDbCommand ComposeSelect(Query query) {
			QSourceName qSourceName = (QSourceName)query.SourceName;
			for (int i=0; i<DataViews.Length; i++)
				if (DataViews[i].MatchSourceName(qSourceName.Name))
					return ComposeDataViewSelect(DataViews[i], query);
			
			return base.ComposeSelect(query);
		}
		
		/// <summary>
		/// </summary>
		protected virtual IDbCommand ComposeDataViewSelect(IDbDataView dataView, Query query) {
			var cmd = DbFactory.CreateCommand();
			
			IDictionary context = BuildSqlCommandContext(cmd, dataView, query);
			cmd.CommandText = dataView.FormatSqlCommandText(context);
			
			return cmd;
		}
		
		/*protected Func<QField,string> InsertFormatter(Func<QField,string> original, Func<QField,string> additional) {
			if (original==null) return additional;
			
			Func<QField,string>[] origFormatters = original is ChainQueryFieldValueFormatter ?
				((ChainQueryFieldValueFormatter)original).Formatters :
				new Func<QField,string>[] { original };
						
			Func<QField,string>[] newFormatters = new Func<QField,string>[origFormatters.Length+1];
			Array.Copy(origFormatters, 0, newFormatters, 1, origFormatters.Length);
			newFormatters[0] = additional;
			return new ChainQueryFieldValueFormatter(newFormatters);
				
		}*/
		
		protected virtual IDictionary BuildSqlCommandContext(IDbCommand cmd, IDbDataView dataView, Query query) {
			var dbSqlBuilder = DbFactory.CreateSqlBuilder(cmd);
			
			// add dataview field formatter in the formatting chain
			var origFormatter =  dbSqlBuilder.QueryFieldValueFormatter;
			var dataViewFormatter = dataView.GetQueryFieldValueFormatter(query);
			dbSqlBuilder.QueryFieldValueFormatter = (qFld) => {
				var res = origFormatter != null ?
					origFormatter(qFld) : qFld.Name;
				if (dataViewFormatter!=null)
					res = dataViewFormatter( (QField) res );
				return res;
			};
					
			string sort = dbSqlBuilder.BuildSort(query);
			string whereExpression = BuildWhereExpression( dbSqlBuilder, dataView, query);
			string fields = dbSqlBuilder.BuildFields(query);

			Hashtable context = (query is Query && ((Query)query).ExtendedProperties != null) ? new Hashtable(((Query)query).ExtendedProperties) : new Hashtable();
			
			BuildNamedQueryNodeContext(context, query.Condition, dbSqlBuilder);
			
			context["whereExpression"] = IsolateWhereExpression( whereExpression );
			context["sortExpression"] = sort;
			context["fields"] = fields;
			context["query"] = query;
			context["sourcename"] = query.SourceName;
			context["startRecord"] = query.StartRecord;
			context["recordCount"] = query.RecordCount;
			context["recordLimit"] = query.StartRecord+query.RecordCount;
			return context;
		}
		
		/// <summary>
		/// Isolates 'where expression' from context where it will be inserted
		/// </summary>
		protected string IsolateWhereExpression(string expression) {
			return expression!=null && expression.Length>0 ? "("+expression+")" : expression;
		}
		
		protected void BuildNamedQueryNodeContext(IDictionary context, QueryNode node, IDbSqlBuilder dbSqlBuilder) {
			if (node==null) return;
			if (!String.IsNullOrEmpty( node.Name )) {
				context[node.Name] = dbSqlBuilder.BuildExpression( node );
			}
			if (node.Nodes!=null)
				foreach (QueryNode childNode in node.Nodes)
					BuildNamedQueryNodeContext(context, childNode, dbSqlBuilder);
		}
		
		protected virtual string BuildWhereExpression(IDbSqlBuilder dbSqlBuilder, IDbDataView dataView, Query query) {
			return dbSqlBuilder.BuildExpression(query.Condition);
		}
		
		
		

		
	}
}
