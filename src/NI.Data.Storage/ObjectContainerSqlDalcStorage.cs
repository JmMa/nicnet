﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;

using NI.Data;
using NI.Data.Storage.Model;

namespace NI.Data.Storage {
	
	public class ObjectContainerSqlDalcStorage : ObjectContainerDalcStorage {

		public string ObjectViewName { get; set; }

		public ObjectContainerSqlDalcStorage(DataRowDalcMapper objectDbMgr, IDalc logDalc, Func<DataSchema> getSchema) :
			base(objectDbMgr, logDalc, getSchema) {
		}

		protected override long[] LoadTranslatedQueryInternal(Class dataClass, Query translatedQuery, Query originalQuery, QSort[] sort) {
			if (String.IsNullOrEmpty(ObjectViewName))
				return base.LoadTranslatedQueryInternal(dataClass, translatedQuery, originalQuery, sort);
			
			translatedQuery.Table = (QTable)ObjectViewName;
			var joinSb = new StringBuilder();
			var sortFields = new List<QSort>();
			if (sort!=null && sort.Length>0) {
				var objTableAlias = originalQuery.Table.Alias ?? ObjectTableName;
				foreach (var origSort in sort) {
					if (origSort.Field.Prefix!=null && origSort.Field.Prefix!=originalQuery.Table.Alias) {
						// related field?
						var relationship = dataClass.Schema.FindRelationshipByID(origSort.Field.Prefix);
						if (relationship!=null) {
							if (relationship.Subject==dataClass || relationship.Object==dataClass) {
								var reversed = relationship.Object==dataClass;
								if (reversed) {
									var revRelationship = dataClass.FindRelationship( relationship.Predicate, relationship.Subject, true );
									if (revRelationship==null)
										throw new ArgumentException(
											String.Format("Relationship {0} cannot be used in reverse direction", relationship.ID) );
									relationship = revRelationship;
								}

								var p = relationship.Object.FindPropertyByID(origSort.Field.Name);
								if (p==null)
									throw new ArgumentException(
										String.Format("Sort field {0} referenced by relationship {1} doesn't exist",
											origSort.Field.Name, origSort.Field.Prefix));
								if (p.Multivalue)
									throw new ArgumentException(
										String.Format("Cannot sort by multivalue property {0}", p.ID));

								// matched related object property
								if (relationship.Multiplicity)
									throw new ArgumentException(
										String.Format("Sorting by relationship {0} is not possible because of multiplicity", origSort.Field.Prefix));									
									
								var propTblName = DataTypeTableNames[p.DataType.ID];
								var propTblAlias = propTblName+"_"+sortFields.Count.ToString();
								var subjFieldName = reversed ? "object_id" : "subject_id";
								var objFieldName = reversed ? "subject_id" : "object_id";

								sortFields.Add( new QSort( propTblAlias+".value", origSort.SortDirection ) );
								joinSb.AppendFormat("LEFT JOIN {0} {1}_rel ON ({1}_rel.{3}={4}.id and {1}_rel.predicate_class_compact_id={2}) ",
									ObjectRelationTableName, propTblAlias, relationship.Predicate.CompactID, subjFieldName, objTableAlias);
								joinSb.AppendFormat("LEFT JOIN {0} {1} ON ({1}.object_id={1}_rel.{4} and {1}.property_compact_id={3}) ",
									propTblName, propTblAlias, objTableAlias, p.CompactID, objFieldName);

								continue;
							} else {
								throw new ArgumentException(
									String.Format("Relationship {0} cannot be used with {1}", relationship.ID, dataClass.ID) );
							}

						}
					}

					if (origSort.Field.Prefix==null || origSort.Field.Prefix==originalQuery.Table.Alias) {
						var p = dataClass.FindPropertyByID(origSort.Field);
						if (p!=null) {
							if (p.Multivalue)
								throw new ArgumentException("Cannot sort by mulivalue property");

							var propTblName = DataTypeTableNames[p.DataType.ID];
							var propTblAlias = propTblName+"_"+sortFields.Count.ToString();
							sortFields.Add( new QSort( propTblAlias+".value", origSort.SortDirection ) );
							joinSb.AppendFormat("LEFT JOIN {0} {1} ON ({1}.object_id={2}.id and {1}.property_compact_id={3}) ",
								propTblName, propTblAlias, objTableAlias, p.CompactID);

							continue;
						}
					}

					sortFields.Add(origSort);

				}
				translatedQuery.Sort = sortFields.ToArray();
				translatedQuery.ExtendedProperties = new Dictionary<string,object>();
				translatedQuery.ExtendedProperties["Joins"] = joinSb.ToString();
			}
			translatedQuery.StartRecord = originalQuery.StartRecord;
			translatedQuery.RecordCount = originalQuery.RecordCount;

			var ids = new List<long>();
			DbMgr.Dalc.ExecuteReader(translatedQuery, (rdr) => {
				while (rdr.Read()) {
					var id = Convert.ToInt64(rdr.GetValue(0));
					ids.Add(id);
				}
			});
			return ids.ToArray();
		}

	}
}