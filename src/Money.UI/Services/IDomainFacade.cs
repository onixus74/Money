﻿using Money.Services.Models;
using Neptuo;
using Neptuo.Activators;
using Neptuo.Models.Keys;
using Neptuo.Models.Repositories;
using Neptuo.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Money.Services
{
    /// <summary>
    /// A command facade for Money domain.
    /// </summary>
    public interface IDomainFacade
    {
        /// <summary>
        /// Gets a factory for creating prices.
        /// </summary>
        IFactory<Price, decimal> PriceFactory { get; }

        /// <summary>
        /// Gets a dispatcher for querying read-models.
        /// </summary>
        IQueryDispatcher QueryDispatcher { get; }

        /// <summary>
        /// Creates an category.
        /// </summary>
        /// <param name="name">A name of the category.</param>
        /// <param name="color">A color of the category.</param>
        /// <returns>Continuation task.</returns>
        Task CreateCategoryAsync(string name, Color color);

        /// <summary>
        /// Creates an outcome.
        /// </summary>
        /// <param name="amount">An amount of the outcome.</param>
        /// <param name="description">A description of the outcome.</param>
        /// <param name="when">A date and time when the outcome occured.</param>
        /// <returns>Continuation task.</returns>
        Task CreateOutcomeAsync(Price amount, string description, DateTime when);

        /// <summary>
        /// Gets a list of outcomes from a category.
        /// </summary>
        /// <param name="categoryKey">A key of the category to filter.</param>
        /// <returns>A list of outcomes from a category.</returns>
        IEnumerable<OutcomeModel> ListOutcomeByCategory(IKey categoryKey);
    }
}