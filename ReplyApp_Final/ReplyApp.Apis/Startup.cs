﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using NoticeApp.Models;
using ReplyApp.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.IO;
using System.Reflection;
using UploadApp.Models;

namespace ReplyApp.Apis
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = "http://localhost:5000/";
                    options.RequireHttpsMetadata = false;
                    options.Audience = "ReplyApp.Apis"; // * 
                });

            #region CORS
            //[CORS][1] CORS »ç¿ë µî·Ï
            //[CORS][1][1] ±âº»: ¸ðµÎ Çã¿ë
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAnyOrigin",
                    builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });
            //[CORS][1][2] Âü°í: ¸ðµÎ Çã¿ë
            services.AddCors(o => o.AddPolicy("AllowAllPolicy", options =>
            {
                options.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
            }));
            //[CORS][1][3] Âü°í: Æ¯Á¤ µµ¸ÞÀÎ¸¸ Çã¿ë
            services.AddCors(o => o.AddPolicy("AllowSpecific", options =>
                    options.WithOrigins("https://localhost:44356")
                           .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                           .WithHeaders("accept", "content-type", "origin", "X-TotalRecordCount")));
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });
            #endregion

            AddDependencyInjectionContainerForNoticeApp(services);
            AddDependencyInjectionContainerForUploadApp(services);
            AddDependencyInjectionContainerForReplyApp(services);

            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = new HeaderApiVersionReader("X-Api-Version");
            });
            services.AddVersionedApiExplorer(
                options =>
                {
                    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                    // note: the specified format code will format the version as "'v'major[.minor][-status]"
                    options.GroupNameFormat = "'v'VVV";

                    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                    // can also be used to control the format of the API version in route templates
                    // options.SubstituteApiVersionInUrl = true;
                });
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen(
                options =>
                {
                    // add a custom operation filter which sets default values
                    options.OperationFilter<SwaggerDefaultValues>();

                    //// integrate xml comments
                    //options.IncludeXmlComments(XmlCommentsFilePath);
                });
        }

        /// <summary>
        /// Q&A(ReplyApp) 관련 의존성(종속성) 주입 관련 코드만 따로 모아서 관리 
        /// </summary>
        private void AddDependencyInjectionContainerForReplyApp(IServiceCollection services)
        {
            // ReplyAppDbContext.cs Inject: New DbContext Add
            services.AddEntityFrameworkSqlServer().AddDbContext<ReplyAppDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // IReplyRepositoryAsync.cs Inject: DI Container에 서비스(리포지토리) 등록 
            services.AddTransient<IReplyRepository, ReplyRepository>();
        }




        /// <summary>
        /// °øÁö»çÇ×(NoticeApp) °ü·Ã ÀÇÁ¸¼º(Á¾¼Ó¼º) ÁÖÀÔ °ü·Ã ÄÚµå¸¸ µû·Î ¸ð¾Æ¼­ °ü¸® 
        /// </summary>
        /// <param name="services"></param>
        private void AddDependencyInjectionContainerForNoticeApp(IServiceCollection services)
        {
            // NoticeAppDbContext.cs Inject: New DbContext Add
            services.AddEntityFrameworkSqlServer().AddDbContext<NoticeAppDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // INoticeRepositoryAsync.cs Inject: DI Container¿¡ ¼­ºñ½º(¸®Æ÷ÁöÅä¸®) µî·Ï 
            services.AddTransient<INoticeRepositoryAsync, NoticeRepositoryAsync>();
        }


        /// <summary>
        /// ÀÚ·á½Ç(UploadApp) °ü·Ã ÀÇÁ¸¼º(Á¾¼Ó¼º) ÁÖÀÔ °ü·Ã ÄÚµå¸¸ µû·Î ¸ð¾Æ¼­ °ü¸® 
        /// </summary>
        /// <param name="services"></param>
        private void AddDependencyInjectionContainerForUploadApp(IServiceCollection services)
        {
            // UploadAppDbContext.cs Inject: New DbContext Add
            services.AddEntityFrameworkSqlServer().AddDbContext<UploadAppDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // INoticeRepositoryAsync.cs Inject: DI Container¿¡ ¼­ºñ½º(¸®Æ÷ÁöÅä¸®) µî·Ï 
            services.AddTransient<IUploadRepository, UploadRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseCors();

            app.UseRouting();
            app.UseAuthentication(); // *
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // 검색어: ASP.NET Core with API Versioning and Swashbuckle
            // https://github.com/microsoft/aspnet-api-versioning/tree/master/samples/aspnetcore/SwaggerSample
            app.UseSwagger();
            app.UseSwaggerUI(
                options =>
                {
                    // build a swagger endpoint for each discovered API version
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                    }
                });
        }

        static string XmlCommentsFilePath
        {
            get
            {
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var fileName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
                return Path.Combine(basePath, fileName);
            }
        }
    }
}
