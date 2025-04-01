using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Interfaces
{
    /// <summary>
    /// Service for entity with Id property
    /// </summary>
    /// <typeparam name="TEntityType"></typeparam>
    /// <typeparam name="TIDType"></typeparam>
    public interface IEntityWithIdService<TEntityType, TEntityIdType>
    {
        List<TEntityType> GetAll();

        /// <summary>
        /// Gets all entities
        /// </summary>
        /// <returns></returns>
        Task<List<TEntityType>> GetAllAsync();

        /// <summary>
        /// Adds entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<TEntityType> AddAsync(TEntityType entity);

        /// <summary>
        /// Updates entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<TEntityType> UpdateAsync(TEntityType entity);

        /// <summary>
        /// Gets entity by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TEntityType?> GetByIdAsync(TEntityIdType id);

        /// <summary>
        /// Gets entities by Ids
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<List<TEntityType>> GetByIdsAsync(List<TEntityIdType> ids);

        Task DeleteByIdAsync(TEntityIdType id);       
    }
}
