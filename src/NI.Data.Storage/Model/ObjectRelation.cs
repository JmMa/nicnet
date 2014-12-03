﻿#region License
/*
 * Open NIC.NET library (http://nicnet.googlecode.com/)
 * Copyright 2013-2014 Vitalii Fedorchenko
 * Copyright 2014 NewtonIdeas
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NI.Data.Storage.Model {
	
	/// <summary>
	/// Represents relation between two objects
	/// </summary>
	public class ObjectRelation {

		public long SubjectID { get; private set; }
		public Relationship Relation { get; private set; }
		public long ObjectID { get; private set; }

		public ObjectRelation(long subjId, Relationship r, long objId) {
			SubjectID = subjId;
			Relation = r;
			ObjectID = objId;
		}

		public override string ToString() {
			return String.Format("[SubjectID={0}; {1}; ObjectID={2}]", SubjectID, Relation.ToString(), ObjectID);
		}
	}

}
