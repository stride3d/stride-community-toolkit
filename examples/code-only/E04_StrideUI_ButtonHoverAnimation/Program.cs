using E04_StrideUI_ButtonHoverAnimation;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

// A main menu whose buttons grow an underline when the pointer is over them. The menu itself is a
// plain Stride UI tree built from code; all the movement lives in ButtonHoverAnimation.cs, a
// SyncScript that lerps each underline's width every frame.
//
// The underline sits inside the button, under the label, as the second child of a vertical StackPanel.
// That is the detail worth copying: because the underline is a normal child of the layout it never
// overlaps anything, and because it keeps its 3px row whatever its width, the button does not resize
// as the underline appears.

const int MenuButtonTextSize = 24;

var underlineColour = new Color(0, 120, 255, 255);

using var game = new Game();

game.Run(new SceneSetup3D { Camera = CameraSetup.Perspective(), Ground = null }.UseSkybox(), start: Start);

void Start(Scene scene)
{
    // Text draws nothing without a font; this one ships with the engine, so the example needs no assets
    var font = game.Content.Load<SpriteFont>("/Stride.Engine/StrideDefaultFont");

    var animation = new ButtonHoverAnimation();

    var menu = new StackPanel
    {
        Orientation = Orientation.Vertical,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    menu.Children.Add(CreateMenuButton("New Game"));
    menu.Children.Add(CreateMenuButton("Continue"));
    menu.Children.Add(CreateMenuButton("Options"));

    var quit = CreateMenuButton("Quit");
    quit.Click += (_, _) => game.Exit();
    menu.Children.Add(quit);

    // A UI is an entity like any other. The script goes on the same entity as the UIComponent, so one
    // script drives the whole menu.
    scene.Entities.Add(new Entity("Menu")
    {
        new UIComponent
        {
            Page = new UIPage { RootElement = menu },
            RenderGroup = RenderGroup.Group31
        },
        animation
    });

    Button CreateMenuButton(string label)
    {
        var text = new TextBlock
        {
            Text = label,
            Font = font,
            TextSize = MenuButtonTextSize,
            TextColor = Color.White,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Starts collapsed to nothing; the script opens it out on hover. Height is fixed so the row it
        // occupies never changes and the button keeps its size.
        var underline = new Border
        {
            BackgroundColor = underlineColour,
            Height = 3,
            Width = 0,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var content = new StackPanel { Orientation = Orientation.Vertical };
        content.Children.Add(text);
        content.Children.Add(underline);

        var button = new Button
        {
            Content = content,
            BackgroundColor = new Color(0, 0, 0, 120),
            Margin = new Thickness(0, 6, 0, 6),
            Padding = new Thickness(24, 10, 24, 12),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        animation.Track(button, underline);

        return button;
    }
}

/*
---example-metadata
slug: stride-ui-button-hover-animation
title:
  en: Stride UI - Button Hover Animation
level: Intermediate
category: UI
complexity: 2
order: 175
description:
  en: |-
    A main menu built from code whose buttons grow a blue underline while the pointer is over them.
    Two things make a hover effect work in Stride's UI, and both are easy to miss. A Button's
    MouseOverState is tested whenever the mouse moves; RequiresMouseOverUpdate, off by default because
    it costs a hit test per element per frame, makes the engine re-test it while the cursor is still,
    so a button that grows under a resting pointer keeps reacting. And the animation is a lerp toward a target width
    rather than a fixed step per frame, so it settles in the same time whatever the frame rate and
    reverses smoothly when the pointer leaves mid-animation.
concepts:
  - "Reacting to the pointer with RequiresMouseOverUpdate and MouseOverState"
  - Animating a UI element from a SyncScript
  - Frame-rate independent movement with a clamped lerp
  - Laying an underline out inside the button so nothing resizes
  - Driving many UI elements from a single script
  - "Using helpers: SceneSetup3D, UseSkybox"
tags:
  - UI
  - Stride UI
  - Animation
  - Button
  - Hover
  - StackPanel
  - SyncScript
related:
  - E04_StrideUI_BasicWindow
  - E04_StrideUI_DragAndDrop
  - E04_CubeClicker
enabled: true
created: 2026-08-31
---
*/