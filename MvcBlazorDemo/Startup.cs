using FD.SampleData.Data;
using MvcBlazorDemo.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MvcBlazorDemo.Data;
using System;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MvcBlazorDemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();

            services
                .AddSingleton<FD.SampleData.Interfaces.IDbContextFactory<ApplicationDbContext>, DbContextFactory<ApplicationDbContext>>()
                // register the method to obtain a new context and creates the database if there is no a previous connection
                .AddScoped(p => p.GetRequiredService<FD.SampleData.Interfaces.IDbContextFactory<ApplicationDbContext>>().CreateContext())
                .AddScoped(p => p.GetRequiredService<FD.SampleData.Interfaces.IDbContextFactory<ApplicationDbContext>>().ConnectionRestricted)
                // custom data access service
                .AddScoped<IDataService, DataService>();

            services.AddDefaultIdentity<IdentityUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;

            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddAuthentication().AddMicrosoftAccount(microsoftOptions =>
            {
                microsoftOptions.ClientId = Configuration["Authentication_Microsoft_ClientId"];
                microsoftOptions.ClientSecret = Configuration["Authentication_Microsoft_ClientSecret"];
            });

            services.AddAuthentication().AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt_Key"]))
                };
            });


            services.AddRazorPages();
            services.AddServerSideBlazor().AddCircuitOptions(o =>
            {
#if DEBUG
                // only add details when debugging
                if (Env.IsDevelopment()) o.DetailedErrors = true;
#endif
            });
            services.AddControllersWithViews();
            services.AddDatabaseDeveloperPageExceptionFilter();

            var razorBuilder = services.AddRazorPages();
#if DEBUG
            if (Env.IsDevelopment())
            {
                razorBuilder.AddRazorRuntimeCompilation();
            }
#endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // custom seeder
            SeedDb(app.ApplicationServices);
            
            // middleware logger
            app.UseRequestResponseLogging();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToController("Blazor", "Home");
            });
        }

        // on generated data seed size will indicate how many records we want to create
        private const int seedSize = 100;

        /// <summary>
        /// Executes database seeder just after all application services has started.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public void SeedDb(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            DbInitializer<ApplicationDbContext>.Initialize(scope, context, seedSize);
        }
    }
}
