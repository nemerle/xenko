// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering.Lights;
using Xenko.Shaders;
using Buffer = Xenko.Graphics.Buffer;

namespace Xenko.Rendering.LightProbes
{
    /// <summary>
    /// Light renderer for clustered shading.
    /// </summary>
    /// <remarks>
    /// Due to the fact that it handles both Point and Spot with a single logic, it doesn't fit perfectly the current logic of one "direct light groups" per renderer.
    /// </remarks>
    public class LightProbeRenderer : LightGroupRendererBase
    {
        private LightProbeShaderGroupData lightprobeGroup;

        public override Type[] LightTypes { get; } = Type.EmptyTypes;

        public LightProbeRenderer()
        {
            IsEnvironmentLight = true;
        }

        public override void Initialize(RenderContext context)
        {
            base.Initialize(context);

            lightprobeGroup = new LightProbeShaderGroupData(context, this);
        }

        public override void Reset()
        {
            base.Reset();

            lightprobeGroup.Reset();
        }

        public override void SetViews(FastList<RenderView> views)
        {
            base.SetViews(views);

            lightprobeGroup.SetViews(views);
        }

        public override void ProcessLights(ProcessLightsParameters parameters)
        {
            lightprobeGroup.AddView(parameters.ViewIndex, parameters.View, parameters.LightIndices.Count);

            foreach (var index in parameters.LightIndices)
            {
                lightprobeGroup.AddLight(parameters.LightCollection[index], null);
            }

            // Consume all the lights
            parameters.LightIndices.Clear();
        }

        public override void UpdateShaderPermutationEntry(ForwardLightingRenderFeature.LightShaderPermutationEntry shaderEntry)
        {
            shaderEntry.EnvironmentLights.Add(lightprobeGroup);
        }

        private class LightProbeShaderGroupData : LightShaderGroupDynamic
        {
            private readonly LightProbeRenderer lightProbeRenderer;
            private readonly RenderContext renderContext;
            private readonly ShaderSource shaderSourceEnabled;
            private readonly ShaderSource shaderSourceDisabled;

            public LightProbeShaderGroupData(RenderContext renderContext, LightProbeRenderer lightProbeRenderer)
                : base(renderContext, null)
            {
                this.renderContext = renderContext;
                this.lightProbeRenderer = lightProbeRenderer;
                shaderSourceEnabled = new ShaderClassSource("LightProbeShader", 3);
                shaderSourceDisabled = new ShaderClassSource("EnvironmentLight");
            }

            public override void UpdateLayout(string compositionName)
            {
                base.UpdateLayout(compositionName);

                // Setup light probe shader only if there is some light probe data
                // TODO: Just like the ForwardLightingRenderFeature access the LightProcessor, accessing the SceneInstance.LightProbeProcessor is not what we want.
                // Ideally, we should send the data the other way around. Let's fix that together when we refactor the lighting at some point.
                var lightProbeRuntimeData = renderContext.SceneInstance?.GetProcessor<LightProbeProcessor>()?.RuntimeData;
                ShaderSource = lightProbeRuntimeData != null ? shaderSourceEnabled : shaderSourceDisabled;
            }
        }
    }
}
