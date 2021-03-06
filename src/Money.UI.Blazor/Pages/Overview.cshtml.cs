﻿using Microsoft.AspNetCore.Components;
using Money.Commands;
using Money.Components;
using Money.Events;
using Money.Models;
using Money.Models.Confirmation;
using Money.Models.Loading;
using Money.Models.Queries;
using Money.Models.Sorting;
using Money.Services;
using Neptuo;
using Neptuo.Commands;
using Neptuo.Events;
using Neptuo.Events.Handlers;
using Neptuo.Models.Keys;
using Neptuo.Queries;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Money.Pages
{
    public class OverviewBase : ComponentBase,
        System.IDisposable,
        OutcomeCardBase.IContext,
        IEventHandler<OutcomeCreated>,
        IEventHandler<OutcomeDeleted>,
        IEventHandler<OutcomeAmountChanged>,
        IEventHandler<OutcomeDescriptionChanged>,
        IEventHandler<OutcomeWhenChanged>
    {
        public CurrencyFormatter CurrencyFormatter { get; private set; }

        [Inject]
        public ICommandDispatcher Commands { get; set; }

        [Inject]
        public IEventHandlerCollection EventHandlers { get; set; }

        [Inject]
        public IQueryDispatcher Queries { get; set; }

        protected string Title { get; set; }

        protected MonthModel MonthModel { get; set; }
        protected IKey CategoryKey { get; set; }
        protected string CategoryName { get; set; }
        protected List<OutcomeOverviewModel> Models { get; set; }
        protected OutcomeOverviewModel Selected { get; set; }

        protected bool IsCreateVisible { get; set; }
        protected bool IsAmountEditVisible;
        protected bool IsDescriptionEditVisible;
        protected bool IsWhenEditVisible;

        protected DeleteContext<OutcomeOverviewModel> Delete { get; } = new DeleteContext<OutcomeOverviewModel>();
        protected LoadingContext Loading { get; } = new LoadingContext();
        protected SortDescriptor<OutcomeOverviewSortType> SortDescriptor { get; set; } = new SortDescriptor<OutcomeOverviewSortType>(OutcomeOverviewSortType.ByWhen, SortDirection.Descending);
        protected int CurrentPageIndex { get; set; }

        [Parameter]
        protected string Year { get; set; }

        [Parameter]
        protected string Month { get; set; }

        [Parameter]
        protected string CategoryGuid { get; set; }

        protected override async Task OnInitAsync()
        {
            BindEvents();
            Delete.Confirmed += async model => await Commands.HandleAsync(new DeleteOutcome(model.Key));
            Delete.MessageFormatter = model => $"Do you really want to delete outcome '{model.Description}'?";

            CategoryKey = Guid.TryParse(CategoryGuid, out var categoryGuid) ? GuidKey.Create(categoryGuid, KeyFactory.Empty(typeof(Category)).Type) : KeyFactory.Empty(typeof(Category));
            MonthModel = new MonthModel(Int32.Parse(Year), Int32.Parse(Month));

            if (!CategoryKey.IsEmpty)
            {
                CategoryName = await Queries.QueryAsync(new GetCategoryName(CategoryKey));
                Title = $"{CategoryName} outcomes in {MonthModel}";
            }
            else
            {
                Title = $"Outcomes in {MonthModel}";
            }

            CurrencyFormatter = new CurrencyFormatter(await Queries.QueryAsync(new ListAllCurrency()));
            Reload();
        }

        protected async void Reload()
        {
            await LoadDataAsync();
            StateHasChanged();
        }

        protected async Task<bool> LoadDataAsync(int pageIndex = 0)
        {
            int prevPageIndex = CurrentPageIndex;
            CurrentPageIndex = pageIndex;
            using (Loading.Start())
            {
                List<OutcomeOverviewModel> models = await Queries.QueryAsync(new ListMonthOutcomeFromCategory(CategoryKey, MonthModel, SortDescriptor, pageIndex));
                if (models.Count > 0 || pageIndex == 0)
                {
                    Models = models;
                    return true;
                }
                else
                {
                    CurrentPageIndex = prevPageIndex;
                    return false;
                }
            }
        }

        protected async void OnSortChanged()
        {
            await LoadDataAsync(CurrentPageIndex);
            StateHasChanged();
        }

        protected void OnActionClick(OutcomeOverviewModel model, ref bool isVisible)
        {
            Selected = model;
            isVisible = true;
            StateHasChanged();
        }

        protected void OnDeleteClick(OutcomeOverviewModel model)
        {
            Delete.Model = model;
            StateHasChanged();
        }

        protected OutcomeOverviewModel FindModel(IEvent payload)
            => Models.FirstOrDefault(o => o.Key.Equals(payload.AggregateKey));

        public void Dispose()
            => UnBindEvents();

        #region Events

        private void BindEvents()
        {
            EventHandlers
                .Add<OutcomeCreated>(this)
                .Add<OutcomeDeleted>(this)
                .Add<OutcomeAmountChanged>(this)
                .Add<OutcomeDescriptionChanged>(this)
                .Add<OutcomeWhenChanged>(this);
        }

        private void UnBindEvents()
        {
            EventHandlers
                .Remove<OutcomeCreated>(this)
                .Remove<OutcomeDeleted>(this)
                .Remove<OutcomeAmountChanged>(this)
                .Remove<OutcomeDescriptionChanged>(this)
                .Remove<OutcomeWhenChanged>(this);
        }

        private Task UpdateModel(IEvent payload, Action<OutcomeOverviewModel> handler)
        {
            OutcomeOverviewModel model = FindModel(payload);
            if (model != null)
            {
                handler(model);
                StateHasChanged();
            }

            return Task.CompletedTask;
        }

        Task IEventHandler<OutcomeCreated>.HandleAsync(OutcomeCreated payload)
        {
            MonthModel payloadMonth = payload.When;
            if (MonthModel == payloadMonth)
                Reload();

            return Task.CompletedTask;
        }

        Task IEventHandler<OutcomeDeleted>.HandleAsync(OutcomeDeleted payload)
        {
            Reload();
            return Task.CompletedTask;
        }

        Task IEventHandler<OutcomeAmountChanged>.HandleAsync(OutcomeAmountChanged payload)
        {
            if (SortDescriptor.Type == OutcomeOverviewSortType.ByAmount)
                Reload();
            else
                UpdateModel(payload, model => model.Amount = payload.NewValue);

            return Task.CompletedTask;
        }

        Task IEventHandler<OutcomeDescriptionChanged>.HandleAsync(OutcomeDescriptionChanged payload)
        {
            if (SortDescriptor.Type == OutcomeOverviewSortType.ByDescription)
                Reload();
            else
                UpdateModel(payload, model => model.Description = payload.Description);

            return Task.CompletedTask;
        }

        Task IEventHandler<OutcomeWhenChanged>.HandleAsync(OutcomeWhenChanged payload)
        {
            if (SortDescriptor.Type != OutcomeOverviewSortType.ByWhen)
            {
                OutcomeOverviewModel model = FindModel(payload);
                if (model != null && model.When.Year == payload.When.Year && model.When.Month == payload.When.Month)
                {
                    model.When = payload.When;
                    StateHasChanged();
                    return Task.CompletedTask;
                }
            }

            Reload();
            return Task.CompletedTask;
        }

        #endregion

        #region OutcomeCardBase.IContext

        void OutcomeCardBase.IContext.EditAmount(OutcomeOverviewModel model)
            => OnActionClick(model, ref IsAmountEditVisible);

        void OutcomeCardBase.IContext.EditDescription(OutcomeOverviewModel model)
            => OnActionClick(model, ref IsDescriptionEditVisible);

        void OutcomeCardBase.IContext.EditWhen(OutcomeOverviewModel model)
            => OnActionClick(model, ref IsWhenEditVisible);

        void OutcomeCardBase.IContext.Delete(OutcomeOverviewModel model)
            => OnDeleteClick(model);

        #endregion
    }
}
