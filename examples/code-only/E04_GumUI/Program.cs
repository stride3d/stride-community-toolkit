using Gum;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using SkiaGum.Helpers;
using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using System.Drawing;

using var game = new Game();

game.Run(new SceneSetup3D { Camera = CameraSetup.Orthographic(), Lighting = null, Skybox = null, Ground = null }, start: Start);

void Start(Scene rootScene)
{
    GumService.Default.Initialize(game);

    CreateUi();
}

void CreateUi()
{
    var mainPanel = new StackPanel();
    mainPanel.AddToRoot();

    // A label the controls below write into
    var label = new Label();
    mainPanel.AddChild(label);
    label.Text = $"I was created at {DateTime.Now}";

    // Buttons, one of them styled through its visual
    var button = new Button();
    mainPanel.AddChild(button);
    button.Text = "Click Me";
    button.Click += (_, _) => label.Text = $"Button clicked @ {DateTime.Now}";

    var disabledButton = new Button();
    mainPanel.AddChild(disabledButton);
    disabledButton.Text = "Disabled Button";
    disabledButton.IsEnabled = false;

    var buttonVisual = (ButtonVisual)button.Visual;
    buttonVisual.BackgroundColor = Color.Red.ToSkia();

    var checkBox = new CheckBox();
    mainPanel.AddChild(checkBox);
    checkBox.Text = "Click Me";
    checkBox.Checked += (_, _) => label.Text = "CheckBox checked";
    checkBox.Unchecked += (_, _) => label.Text = "CheckBox unchecked";

    var comboBox = new ComboBox();
    var items = comboBox.Items ?? throw new InvalidOperationException("The combo box has no item list.");

    for (var i = 0; i < 10; i++)
    {
        items.Add($"Item {i}");
    }

    comboBox.SelectionChanged += (_, _) => label.Text = "Selected: " + comboBox.SelectedObject;
    mainPanel.AddChild(comboBox);

    var listBox = new ListBox();
    listBox.Visual.Width = 150;
    listBox.Height = 100;

    for (var i = 0; i < 10; i++)
    {
        listBox.Items?.Add($"Item {i}");
    }

    listBox.SelectionChanged += (_, _) => label.Text = $"Selected item is {listBox.SelectedObject} at index {listBox.SelectedIndex}";
    mainPanel.AddChild(listBox);
}

/*
---example-metadata
slug: gum-stride-ui-basic
title:
  en: Basic Gum UI Setup
level: Beginner
category: UI
complexity: 1
order: 12
description:
  en: |-
    Initialise Gum UI in Stride using the official Gum.Stride runtime and the Stride Community Toolkit.
    Demonstrates setting up GumService with the Game instance, whose renderer ticks the UI every frame,
    and building interactive UI layouts in C# with Gum Forms controls such as StackPanel, Label, Button,
    CheckBox, ComboBox, and ListBox, complete with styling and event handling.
concepts:
  - Initialising Gum UI via GumService.Default.Initialize(game)
  - "No per-frame Update call: the renderer Initialize registers ticks Gum itself"
  - Setting up scene graphics compositing and a 2D camera with Stride Community Toolkit
  - Constructing UI hierarchy using Gum.Forms controls (StackPanel, Label, Button, CheckBox, ComboBox, ListBox)
  - Handling UI events and modifying visual properties such as background colour
tags:
  - UI
  - Gum UI
  - Gum.Stride
  - Gum Forms
  - Community Toolkit
  - 2D
  - Controls
related:
tocName: Basic Gum UI
enabled: true
created: 2026-09-04
---
*/