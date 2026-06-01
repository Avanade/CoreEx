global using app-name.Contracts;
global using CoreEx;
global using CoreEx.Data;
global using CoreEx.DependencyInjection;
global using CoreEx.Entities;
global using CoreEx.Events;
global using CoreEx.Localization;
#if (refdata-enabled)
global using app-name.Application.Repositories;
global using CoreEx.RefData;
global using CoreEx.RefData.Abstractions;
#endif
#if (rop-enabled)
global using CoreEx.Results;
#endif
global using CoreEx.Validation;