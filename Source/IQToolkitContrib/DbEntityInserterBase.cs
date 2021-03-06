﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;

namespace IQToolkitContrib {
    public abstract class DbEntityInserterBase<T> {
        protected readonly IEntityTable<T> entityTable;
        protected readonly Type entityType = typeof(T);
        protected readonly PropertyInfo primaryKey;
        protected readonly Func<T, int> func;
        private readonly MappingEntity mappingEntity;

        protected DbEntityInserterBase(IEntityTable<T> entityTable, DbEntityProvider provider) {
            if (entityTable == null) {
                throw new ArgumentNullException("entityTable");
            }

            this.entityTable = entityTable;

            if (provider != null) {
                this.mappingEntity = provider.Mapping.GetEntity(this.entityType);
                this.primaryKey = this.GetPrimaryKey(provider);
            }

            this.func = this.GetFunction();
        }

        /// <summary>
        /// Executes the Insert for the Entity.
        /// </summary>
        /// <param name="instance">Entity instance.</param>
        /// <returns>Returns the Insert row count.</returns>
        public int Execute(T instance) {
            return this.func(instance);
        }

        /// <summary>
        /// Inspects the Entity mapping information to determine if the standard Insert statement should be used or if the insert statement should include 
        /// the extra step of retrieving the AutoGenerated primary key.
        /// </summary>
        /// <returns>Returns the Insert function.</returns>
        protected abstract Func<T, int> GetFunction();

        /// <summary>
        /// Gets a reference to the IQToolkit.Updatable.Insert method.
        /// </summary>
        /// <param name="genericTypes">Generic Types Array for the Expression.</param>
        /// <returns>Returns IQToolkit.Updatable.Insert MethodInfo.</returns>
        protected abstract MethodInfo GetMethodInfo(Type[] genericTypes);

        /// <summary>
        /// Creates an Expression that will be passed to the IQToolkit.Updatable.Insert method
        /// </summary>
        /// <param name="genericTypes">Generic Types Array for the Expression.</param>
        /// <returns>Returns an Expression.</returns>
        protected LambdaExpression GetExpression(Type[] genericTypes) {
            // Create type for Expression<Func<T, S>>.
            Type functionExpressionType = Expression.GetFuncType(genericTypes);

            // Create an Expression Parameter.
            var parameter = Expression.Parameter(this.entityType, "instance");

            // Create a Lambda Expression for the Func<T, S> call. 
            // Expression example:  x => x.Id
            return Expression.Lambda(functionExpressionType, Expression.Property(parameter, this.primaryKey.Name), parameter);
        }

        /// <summary>
        /// Use reflection to determine if the primary key column was mapped as a generated column.
        /// </summary>
        /// <returns>Returns true if the primary key is generated by the database.</returns>
        protected bool IsGeneratedPrimaryKey() {
            // Get a reference to the internal method GetMappingMember.
            MethodInfo getMappingMember = this.mappingEntity.GetType()
                                                             .GetMethod("GetMappingMember", BindingFlags.NonPublic | BindingFlags.Instance);

            // Invoke GetMappingMember.
            object mappingMember = getMappingMember.Invoke(this.mappingEntity, new object[] { this.primaryKey.Name });

            // Get the Column property from the mappingMember.
            ColumnAttribute columnAttribute = (ColumnAttribute)mappingMember.GetType()
                                                                            .GetProperty("Column", BindingFlags.NonPublic | BindingFlags.Instance)
                                                                            .GetValue(mappingMember);

            return columnAttribute.IsGenerated;
        }

        /// <summary>
        /// Gets the primary key if the Entity has only one column for the primary key.  The assumption is that composite
        /// primary keys don't need to be retrieved after executing the Insert statement.
        /// </summary>
        /// <param name="provider">DbEntityProvider instance.</param>
        /// <returns>Returns the PrimaryKey PropertyInfo.</returns>
        private PropertyInfo GetPrimaryKey(DbEntityProvider provider) {
            MemberInfo primaryKeyMemberInfo = provider.Mapping
                                                      .GetPrimaryKeyMembers(this.mappingEntity)
                                                      .SingleOrDefault();

            return primaryKeyMemberInfo as PropertyInfo;
        }
    }
}