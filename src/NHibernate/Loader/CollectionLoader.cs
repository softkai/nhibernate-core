using System;
using System.Text;
using System.Collections;

using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Persister;
using NHibernate.Sql;
using NHibernate.SqlCommand;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Loader {
	/// <summary>
	/// Loads a collection of values or a many-to-many association
	/// </summary>
	public class CollectionLoader : OuterJoinLoader, ICollectionInitializer {
		private CollectionPersister collectionPersister;
		private IType idType;
		
		public CollectionLoader(CollectionPersister persister, ISessionFactoryImplementor factory) : base(factory.Dialect) {
			
			idType = persister.KeyType;

			string alias = Alias( persister.QualifiedTableName, 0);

			//string whereString="";
			//if (persister.HasWhere) whereString = " and " + persister.GetSQLWhereString(alias);

			SqlString whereSqlString = null;

			if (persister.HasWhere) 
				whereSqlString = new SqlString(persister.GetSQLWhereString(alias));
				
			IList associations = WalkCollectionTree(persister, alias, factory);

			int joins = associations.Count;
			suffixes = new string[joins];
			for (int i=0; i<joins; i++) suffixes[i] = i.ToString() + StringHelper.Underscore;

			JoinFragment ojf = OuterJoins(associations);
			
			SqlSelectBuilder selectBuilder = new SqlSelectBuilder(factory);
			selectBuilder.SetSelectClause(
				persister.SelectClauseFragment(alias) + 
				(joins==0 ? String.Empty: ", " + SelectString(associations))
				)
				.SetFromClause(persister.QualifiedTableName, alias)
				.SetWhereClause(alias, persister.KeyColumnNames, persister.KeyType)
				.SetOuterJoins(ojf.ToFromFragmentString, ojf.ToWhereFragmentString);
			
			if(persister.HasWhere) selectBuilder.AddWhereClause(whereSqlString);
				
			if(persister.HasOrdering) selectBuilder.SetOrderByClause(persister.GetSQLOrderByString(alias));

			this.sqlString = selectBuilder.ToSqlString();
			
//			Select select = new Select()
//				.SetSelectClause(
//					persister.SelectClauseFragment(alias) +
//					(joins==0 ? String.Empty : ", " + SelectString(associations) )
//					)
//				.SetFromClause ( persister.QualifiedTableName, alias )
//				.SetWhereClause(
//					new ConditionalFragment().SetTableAlias(alias)
//					.SetCondition( persister.KeyColumnNames, StringHelper.SqlParameter )
//					.ToFragmentString() +
//					whereString
//				)
//				.SetOuterJoins(
//					ojf.ToFromFragmentString,
//					ojf.ToWhereFragmentString
//				);
//			if (persister.HasOrdering) select.SetOrderByClause( persister.GetSQLOrderByString(alias));
//			sql = select.ToStatementString();

			classPersisters = new ILoadable[joins];
			for (int i=0; i<joins; i++) classPersisters[i] = (ILoadable) ((OuterJoinableAssociation) associations[i]).Subpersister;
			this.collectionPersister = persister;

			PostInstantiate();
			
		}

		protected override CollectionPersister CollectionPersister {
			get { return collectionPersister; }
		}

		public void Initialize(object id, PersistentCollection collection, object owner, ISessionImplementor session) {
			LoadCollection(session, id, idType, owner, collection);
		}
	}
}
