using Example17_SignalR_Blazor.Components;
using Example17_SignalR_Blazor.Hubs;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

var app = builder.Build();

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<Screen1Hub>(Constants.HubUrl);

app.Run();
/*
---example-metadata
slug: stride-signalr-server
title:
  en: Stride + SignalR - Blazor Server
level: Advanced
category: Networking
complexity: 3
order: 170
description:
  en: |-
    The other half of the SignalR example: a minimal Blazor server hosting the hub and a page with the
    buttons that drive the game. It is an ASP.NET host rather than a Stride app, so there is no scene
    and nothing to render - it is listed here only because it has to be started first, and it is
    documented as part of stride-signalr rather than on a page of its own.
concepts:
  - Hosting a SignalR hub in a minimal Blazor server app
  - Exposing hub methods a game client can call
  - Sending commands to connected clients from a web page
  - "Start this before the Stride app: the game connects to the hub"
tags:
  - Networking
  - SignalR
  - Blazor
  - ASP.NET
  - Server
  - Multi Project
related:
  - Example17_SignalR
docs: false
screenshot: false
enabled: true
created: 2025-05-04
---
*/