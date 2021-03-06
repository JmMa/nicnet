﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;

using NUnit.Framework;
using NI.Data;

using NI.Data.Storage.Model;

namespace NI.Data.Storage.Tests {
	
	[TestFixture]
	public class SQLiteStorageDalcTests {
		
		SQLiteStorageContext StorageContext;

		[SetUp]
		public void SetUp() {
			StorageContext = new SQLiteStorageContext( (StorageDbMgr, ObjectContainerStorage) => {
				return new MetadataTableSchemaStorage(StorageDbMgr);	
			});
			StorageContext.CreateTestDataSchema();

			Logger.SetInfo((t, msg) => {
				Console.WriteLine("[{0}] {1}", t, msg);
			});
		}

		[TearDown]
		public void CleanUp() {
			StorageContext.Destroy();
		}

		protected void addTestData() {
			var testSchema = StorageContext.DataSchemaStorage.GetSchema();

			var googCompany = new ObjectContainer(testSchema.FindClassByID("companies"));
			googCompany["name"] = "Google";
			googCompany["created"] = new DateTime(1998, 9, 4, 9, 30, 0);
			
			var msCompany = new ObjectContainer(testSchema.FindClassByID("companies"));
			msCompany["name"] = "Microsoft";
			msCompany["created"] = new DateTime(1975, 4, 4, 10, 0, 0);
			
			StorageContext.ObjectContainerStorage.Insert(googCompany);
			StorageContext.ObjectContainerStorage.Insert(msCompany);

			var johnContact = new ObjectContainer(testSchema.FindClassByID("contacts"));
			johnContact["name"] = "John";
			johnContact["is_primary"] = true;
			var maryContact = new ObjectContainer(testSchema.FindClassByID("contacts"));
			maryContact["name"] = "Mary";
			maryContact["is_primary"] = false;
			maryContact["birthday"] = new DateTime(1999, 5, 20);
			var bobContact = new ObjectContainer(testSchema.FindClassByID("contacts"));
			bobContact["name"] = "Bob";
			bobContact["is_primary"] = true;

			StorageContext.ObjectContainerStorage.Insert(johnContact);
			StorageContext.ObjectContainerStorage.Insert(maryContact);
			StorageContext.ObjectContainerStorage.Insert(bobContact);

			var usaCountry = new ObjectContainer(testSchema.FindClassByID("countries"));
			usaCountry["name"] = "USA";
			var canadaCountry = new ObjectContainer(testSchema.FindClassByID("countries"));
			canadaCountry["name"] = "Canada";
			StorageContext.ObjectContainerStorage.Insert(usaCountry);
			StorageContext.ObjectContainerStorage.Insert(canadaCountry);

			var rel = testSchema.FindClassByID("contacts").FindRelationship(
				testSchema.FindClassByID("employee"), testSchema.FindClassByID("companies"));
			var countryRel = testSchema.FindRelationshipByID("companies_country_countries");
			
			StorageContext.ObjectContainerStorage.AddRelation( 
				new ObjectRelation( johnContact.ID.Value, rel, googCompany.ID.Value )
			);
			StorageContext.ObjectContainerStorage.AddRelation(
				new ObjectRelation(bobContact.ID.Value, rel, msCompany.ID.Value)
			);

			StorageContext.ObjectContainerStorage.AddRelation(
				new ObjectRelation(msCompany.ID.Value, countryRel, usaCountry.ID.Value)
			);
			StorageContext.ObjectContainerStorage.AddRelation(
				new ObjectRelation(googCompany.ID.Value, countryRel, canadaCountry.ID.Value)
			);

		}

		[Test]
		public void Insert() {
			var testSchema = StorageContext.DataSchemaStorage.GetSchema();
			// add using DataRow
			
			var ds = new DataSet();
			var contactsTbl = testSchema.FindClassByID("contacts").CreateDataTable();
			ds.Tables.Add(contactsTbl);

			for (int i=0; i<5; i++) {
				var r = contactsTbl.NewRow();
				r["name"] = "Contact "+i.ToString();
				contactsTbl.Rows.Add(r);
			}

			StorageContext.StorageDalc.Update(contactsTbl);
			// should be 5 contacts
			Assert.AreEqual(5, StorageContext.ObjectContainerStorage.GetObjectsCount( new Query("contacts") ) );

			// quick insert
			StorageContext.StorageDalc.Insert("companies", new {
				name = "TestCompany 1"
			});
			Assert.AreEqual(1, StorageContext.ObjectContainerStorage.GetObjectsCount(new Query("companies")));
			StorageContext.StorageDalc.Insert("companies", new {
				name = "TestCompany 2"
			});
			Assert.AreEqual(2, StorageContext.ObjectContainerStorage.GetObjectsCount(new Query("companies")));

			// test insert relation using datarow
			var rel = testSchema.FindRelationshipByID("contacts_employee_companies");
			var relTbl = rel.CreateDataTable();
			ds.Tables.Add(relTbl);
			var relRow = relTbl.NewRow();
			relRow["subject_id"] = contactsTbl.Rows[0]["id"];
			relRow["object_id"] = StorageContext.StorageDalc.LoadValue(
				new Query("companies", (QField)"name" == (QConst)"TestCompany 1") { Fields = new[]{(QField)"id"} } );
			relTbl.Rows.Add(relRow);

			StorageContext.StorageDalc.Update(relTbl);

			Assert.AreEqual(1, 
				StorageContext.ObjectContainerStorage.LoadRelations( 
					new ObjectContainer(testSchema.FindClassByID("contacts"), Convert.ToInt64(contactsTbl.Rows[0]["id"]) ), null ).Count() );

			// quick relation insert
			StorageContext.StorageDalc.Insert("contacts_employee_companies", new {
				subject_id = contactsTbl.Rows[1]["id"],
				object_id = StorageContext.StorageDalc.LoadValue(
				new Query("companies", (QField)"name" == (QConst)"TestCompany 2") { Fields = new[] { (QField)"id" } }) } );

			Assert.AreEqual(1,
				StorageContext.ObjectContainerStorage.LoadRelations(
					new ObjectContainer(testSchema.FindClassByID("contacts"), Convert.ToInt64(contactsTbl.Rows[1]["id"])), null).Count());
		}

		[Test]
		public void Update() {
			addTestData();
			var testSchema = StorageContext.DataSchemaStorage.GetSchema();

			// datarow update
			var ds = new DataSet();
			ds.Tables.Add(testSchema.FindClassByID("contacts").CreateDataTable());
			var contactsTbl = StorageContext.StorageDalc.Load( new Query("contacts", (QField)"name"==(QConst)"Bob" ), ds);
			contactsTbl.Rows[0]["name"] = "Bob1";
			contactsTbl.Rows[0]["birthday"] = new DateTime(1985, 2, 2);
			StorageContext.StorageDalc.Update(contactsTbl);

			var bob1Contact = StorageContext.StorageDalc.LoadRecord(new Query("contacts", (QField)"id" == new QConst(contactsTbl.Rows[0]["id"])));
			Assert.NotNull(bob1Contact);
			Assert.AreEqual("Bob1", bob1Contact["name"]);
			Assert.AreEqual(new DateTime(1985, 2, 2), bob1Contact["birthday"]);

			// quick update
			Assert.AreEqual(1, StorageContext.StorageDalc.Update( new Query("contacts",  (QField)"name" == new QConst("Bob1") ), new {
				name = "Bob2", birthday = (string)null
			}) );

			var bobObj = StorageContext.ObjectContainerStorage.Load( new [] {  Convert.ToInt64(contactsTbl.Rows[0]["id"]) } ).Values.First();
			Assert.AreEqual("Bob2", bobObj["name"] );
			Assert.AreEqual(null, bobObj["birthday"]);


		}

		[Test]
		public void Delete() {
			addTestData();
			var testSchema = StorageContext.DataSchemaStorage.GetSchema();

			// datarow delete
			var ds = new DataSet();
			ds.Tables.Add(testSchema.FindClassByID("contacts").CreateDataTable());
			var contactsTbl = StorageContext.StorageDalc.Load(new Query("contacts"), ds);
			Assert.AreEqual(3, contactsTbl.Rows.Count);

			contactsTbl.Rows[0].Delete();
			StorageContext.StorageDalc.Update(contactsTbl);
			Assert.AreEqual(2, StorageContext.ObjectContainerStorage.GetObjectsCount( new Query("contacts") ) );

			// delete by query
			StorageContext.StorageDalc.Delete(new Query("contacts", (QField)"name" == new QConst(contactsTbl.Rows[0]["name"])));
			Assert.AreEqual(1, StorageContext.ObjectContainerStorage.GetObjectsCount(new Query("contacts")));

			// delete relation row
			var rel = testSchema.FindRelationshipByID("contacts_employee_companies");
			var relTbl = rel.CreateDataTable();
			ds.Tables.Add(relTbl);
			StorageContext.StorageDalc.Load( new Query("contacts_employee_companies"), ds);
			Assert.AreEqual(1, relTbl.Rows.Count );

			relTbl.Rows[0].Delete();
			StorageContext.StorageDalc.Update(relTbl);

			Assert.AreEqual(0, StorageContext.ObjectContainerStorage.LoadRelations( "contacts_employee_companies", null ).Count() );
		}


		[Test]
		public void LoadAndSubquery() {
			addTestData();
			var storageDalc = StorageContext.StorageDalc;

			var primaryContacts = storageDalc.LoadAllRecords( new Query("contacts", (QField)"is_primary"==new QConst(true) ) );
			Assert.AreEqual(2, primaryContacts.Length);
			Assert.True( primaryContacts.Where(r=>r["name"].ToString()=="John").Any() );
			Assert.True(primaryContacts.Where(r => r["name"].ToString() == "Bob").Any());

			// load only some fields (including related field
			var ds = new DataSet();
			var contactsTbl = storageDalc.Load( new Query("contacts") { 
					Fields = new[] { (QField)"name", (QField)"contacts_employee_companies.name" } 
				}, ds );
			Assert.AreEqual(2, contactsTbl.Columns.Count );
			Assert.AreEqual(3, contactsTbl.Rows.Count );
			Assert.AreEqual("name", contactsTbl.Columns[0].ColumnName);
			Assert.AreEqual("contacts_employee_companies_name", contactsTbl.Columns[1].ColumnName);
			var expectedCompanyNames = new Dictionary<string,object>(){
				{"John","Google"},
				{"Mary", DBNull.Value},
				{"Bob", "Microsoft"}
			};
			foreach (DataRow r in contactsTbl.Rows) {
				Assert.AreEqual( expectedCompanyNames[r["name"].ToString()], r["contacts_employee_companies_name"] );
			}
			// test expression-driven "as" syntax for loading related fields
			var relatedCompanyNameFld = storageDalc.LoadValue( new Query("contacts", (QField)"name" == new QConst("Bob") ) { 
					Fields = new[] { new QField("company_name", "contacts_employee_companies.name") } 
				});
			Assert.AreEqual("Microsoft", relatedCompanyNameFld );

			Assert.AreEqual( new DateTime(1999, 5, 20), 
				storageDalc.LoadValue( new Query("contacts", (QField)"name"==(QConst)"Mary" ) {
					Fields = new[] { (QField)"birthday" }
				} ) );

			// sort 
			var companies = storageDalc.LoadAllRecords( new Query("companies") { 
				Sort = new[] { new QSort("name", System.ComponentModel.ListSortDirection.Descending) } } );
			Assert.AreEqual("Microsoft", companies[0]["name"] );
			Assert.AreEqual("Google", companies[1]["name"]);

			var sortedContactsQuery = new Query("contacts") {
				Fields = new [] { (QField)"id" },
				Sort = new[] { 
					new QSort("birthday", System.ComponentModel.ListSortDirection.Ascending),
					new QSort("is_primary", System.ComponentModel.ListSortDirection.Descending),
					new QSort("name", System.ComponentModel.ListSortDirection.Descending) }
				};
			var sortedContactIds = storageDalc.LoadAllValues(sortedContactsQuery);

			Assert.AreEqual(3, sortedContactIds[0]);
			Assert.AreEqual(5, sortedContactIds[1]);
			Assert.AreEqual(4, sortedContactIds[2]);

			sortedContactsQuery.StartRecord = 1;
			sortedContactsQuery.RecordCount = 1;
			var pagedContactIds = storageDalc.LoadAllValues( sortedContactsQuery );
			Assert.AreEqual(1, pagedContactIds.Length);
			Assert.AreEqual(5, pagedContactIds[0]);

			// load relation
			var googContactIds = storageDalc.LoadAllValues(new Query("contacts_employee_companies",
				(QField)"object_id" == new QConst(1) ) { Fields = new[] {(QField)"subject_id"} } );
			Assert.AreEqual(1, googContactIds.Length );
			Assert.AreEqual(3, googContactIds[0]);

			// load with subquery
			Assert.AreEqual("Google", StorageContext.StorageDalc.LoadValue(
						new Query("companies",
							new QueryConditionNode((QField)"id", Conditions.In,
								new Query("contacts_employee_companies",
									new QueryConditionNode(
										(QField)"subject_id", Conditions.In,
										new Query("contacts", new QueryConditionNode((QField)"name", Conditions.Like, (QConst)"Jo%")) {
											Fields = new[] { (QField)"id" }
										}
									)
								) {
									Fields = new[] { (QField)"object_id" }
								}
							)
						) { Fields = new[] { (QField)"name" } }
					));

			// load by related field (identical to query above)
			Assert.AreEqual("Google", StorageContext.StorageDalc.LoadValue(
				new Query("companies",
					new QueryConditionNode((QField)"contacts_employee_companies.name",Conditions.Like, (QConst)"Jo%")
				) { Fields = new[] { (QField)"name" } }
			));

			// sort by related field
			var contactsByCompanyName = StorageContext.StorageDalc.LoadAllRecords( new Query("contacts") {
				Sort = new[] { (QSort)"contacts_employee_companies.name asc" },
				Fields = new [] { (QField)"id", (QField)"name", (QField)"contacts_employee_companies.companies_country_countries.name" }
			});
			Assert.AreEqual(3, contactsByCompanyName.Length);
			Assert.AreEqual("Mary", contactsByCompanyName[0]["name"]);
			Assert.AreEqual(DBNull.Value, contactsByCompanyName[0]["contacts_employee_companies_companies_country_countries_name"]);
			Assert.AreEqual("John", contactsByCompanyName[1]["name"]);
			Assert.AreEqual("Canada", contactsByCompanyName[1]["contacts_employee_companies_companies_country_countries_name"]);
			Assert.AreEqual("Bob", contactsByCompanyName[2]["name"]);
			Assert.AreEqual("USA", contactsByCompanyName[2]["contacts_employee_companies_companies_country_countries_name"]);

			// order by rel
			var contactsByCompanyNameDesc = StorageContext.StorageDalc.LoadAllRecords(new Query("contacts") {
				Sort = new[] { (QSort)"contacts_employee_companies.name desc" }
			});
			Assert.AreEqual(3, contactsByCompanyNameDesc.Length);
			Assert.AreEqual("Bob", contactsByCompanyNameDesc[0]["name"]);
			Assert.AreEqual("John", contactsByCompanyNameDesc[1]["name"]);
			Assert.AreEqual("Mary", contactsByCompanyNameDesc[2]["name"]);

			// order by inferred rel
			var contactsByCompanyCountryAsc = StorageContext.StorageDalc.LoadAllRecords(new Query("contacts") {
				Fields = new[] { 
					(QField)"name", 
					(QField)"contacts_employee_companies.companies_country_countries.name",
					(QField)"id", // object id
					(QField)"contacts_employee_companies.id", // related object id
					(QField)"contacts_employee_companies.companies_country_countries.id"
				 },
				Sort = new[] { (QSort)"contacts_employee_companies.companies_country_countries.name asc" }
			});
			Assert.AreEqual(3, contactsByCompanyCountryAsc.Length);
			Assert.AreEqual(DBNull.Value, contactsByCompanyCountryAsc[0]["contacts_employee_companies_companies_country_countries_name"]);
			Assert.AreEqual("Canada", contactsByCompanyCountryAsc[1]["contacts_employee_companies_companies_country_countries_name"]);
			Assert.AreEqual("USA", contactsByCompanyCountryAsc[2]["contacts_employee_companies_companies_country_countries_name"]);
			// check ids
			Assert.True( contactsByCompanyCountryAsc[1]["id"] is long );
			Assert.True(contactsByCompanyCountryAsc[1]["contacts_employee_companies_id"] is long);
			Assert.True(contactsByCompanyCountryAsc[1]["contacts_employee_companies_companies_country_countries_id"] is long);

			// filter by inferred rel
			Assert.AreEqual("Bob", StorageContext.StorageDalc.LoadValue(
				new Query("contacts", (QField)"contacts_employee_companies.companies_country_countries.name"==(QConst)"USA" ) {
					Fields = new[] {(QField)"name"}
				}
			));

			// filter by id
			Assert.AreEqual("Mary", StorageContext.StorageDalc.LoadValue(
				new Query("contacts", (QField)"id"==new QConst(contactsByCompanyName[0]["id"]) ) {
					Fields = new[] {(QField)"name"}
				}
			));

			// filter by null value
			Assert.AreEqual(2, StorageContext.StorageDalc.RecordsCount(
					new Query("contacts", 
						new QueryConditionNode( (QField)"birthday", Conditions.Null, null ) )
				) );
		}

		[Test]
		public void SchemaDataSetFactory() {
			var schemaDsFactory = new SchemaDataSetFactory( StorageContext.DataSchemaStorage.GetSchema );
			var ds1 = schemaDsFactory.GetDataSet("contacts");
			Assert.True(ds1.Tables.Contains("contacts"));
			Assert.AreEqual(4, ds1.Tables["contacts"].Columns.Count );

			var dataRowMapper = new DataRowDalcMapper(StorageContext.StorageDalc, schemaDsFactory.GetDataSet);
			var testContactRow = dataRowMapper.Create("contacts");
			testContactRow["name"] = "Test1";
			dataRowMapper.Update(testContactRow);

			var loadedTestContactRow = dataRowMapper.Load("contacts", testContactRow["id"]);
			Assert.NotNull(loadedTestContactRow);
			Assert.AreEqual("Test1", loadedTestContactRow["name"]);
		}


		[Test]
		public void DerivedProperty() {
			addTestData();
			var storageDalc = StorageContext.StorageDalc;
			var testSchema = StorageContext.DataSchemaStorage.GetSchema();

			var birthdayYear = new Property("birthday_year");
			birthdayYear.CompactID = -1;
			birthdayYear.DataType = PropertyDataType.Integer;
			testSchema.AddProperty( birthdayYear );
			var birthdayClassProp = testSchema.FindClassPropertyLocation("contacts","birthday");
			testSchema.AddClassProperty(
				new ClassPropertyLocation(testSchema.FindClassByID("contacts"), birthdayYear, birthdayClassProp, "getDateYear"));

			var idDerived = new Property("id_derived2");
			idDerived.CompactID = -2;
			idDerived.DataType = PropertyDataType.Integer;
			testSchema.AddProperty( idDerived );
			var nameClassProp = testSchema.FindClassPropertyLocation("contacts","id");
			testSchema.AddClassProperty(
				new ClassPropertyLocation(testSchema.FindClassByID("contacts"), idDerived, nameClassProp, "{0}*10"));

			/*var createdYear = new Property("created_year");
			createdYear.CompactID = -2;
			createdYear.DataType = PropertyDataType.Integer;
			testSchema.AddProperty( createdYear );
			var createdClassProp = testSchema.FindClassPropertyLocation("companies","created");
			testSchema.AddClassProperty(
				new ClassPropertyLocation(testSchema.FindClassByID("companies"), createdYear, createdClassProp, "getDateYear"));*/

			// filter by derived prop
			Assert.AreEqual("Mary", storageDalc.LoadValue( 
				new Query("contacts", (QField)"birthday_year"==(QConst)1999 ) {
					Fields = new[] {(QField)"name"}
				}) );

			// select derived prop
			Assert.AreEqual(1999, storageDalc.LoadValue( 
				new Query("contacts", (QField)"name"==(QConst)"Mary" ) {
					Fields = new[] {(QField)"birthday_year"}
				}) );

			var maryInfo = storageDalc.LoadRecord( 
				new Query("contacts", (QField)"name"==(QConst)"Mary" ) {
					Fields = new[] {(QField)"id_derived2", (QField)"id"}
				});
			Assert.AreEqual( Convert.ToInt64( maryInfo["id"] )*10, maryInfo["id_derived2"] );

			// filter by related derived prop
			Assert.AreEqual("Bob", StorageContext.StorageDalc.LoadValue(
				new Query("contacts", (QField)"contacts_employee_companies.created_year"==(QConst)1975 ) {
					Fields = new[] {(QField)"name"}
				}
			));

			// select related derived prop
			Assert.AreEqual(1998, StorageContext.StorageDalc.LoadValue(
				new Query("contacts", (QField)"name"==(QConst)"John" ) {
					Fields = new[] {(QField)"contacts_employee_companies.created_year"}
				}
			));

			// order by related derived prop
			var allNames = StorageContext.StorageDalc.LoadAllValues(
				new Query("contacts") {
					Fields = new[] {(QField)"name"},
					Sort = new[] { (QSort)"contacts_employee_companies.created_year" }
				}
			);
			Assert.AreEqual("Mary",allNames[0]);
			Assert.AreEqual("Bob",allNames[1]);


		}

	}
}
