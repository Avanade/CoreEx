﻿using CoreEx.FluentValidation;
using CoreEx.AspNetCore.WebApis;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Services;
using CoreEx.TestFunction.Validators;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace CoreEx.TestApi.Controllers
{
    [ApiController]
    [Route("products")]
    public class ProductController : ControllerBase
    {
        private readonly WebApi _webApi;
        private readonly ProductService _service;

        public ProductController(WebApi webApi, ProductService service)
        {
            _webApi = webApi;
            _service = service;
        }

        [HttpGet]
        [Route("{id}")]
        public Task<IActionResult> GetAsync(string id) => _webApi.GetAsync(Request, _ => _service.GetProductAsync(id));

        [HttpPost]
        public Task<IActionResult> PostAsync() => _webApi.PostAsync<Product, Product>(Request, r => _service.AddProductAsync(r.Value), validator: new ProductValidator().Wrap());

        [HttpPut]
        [Route("{id}")]
        public Task<IActionResult> PutAsync(string id) => _webApi.PutAsync<Product, Product>(Request, r => _service.UpdateProductAsync(r.Value, id));

        [HttpDelete]
        [Route("{id}")]
        public Task<IActionResult> DeleteAsync(string id) => _webApi.DeleteAsync(Request, _ => _service.DeleteProductAsync(id));

        [HttpGet]
        [Route("{id}/catalogue")]
        public Task<IActionResult> GetCatalogueAsync(string id) => _webApi.GetAsync<FileContentResult>(Request, _ => _service.GetCatalogueAsync(id));
    }
}