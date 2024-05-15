namespace SceneService.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using SceneService.Controllers;

    public class ControllerProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            var controllersToInclude = new List<Type>
            {
                typeof(SceneController),
            };

            feature.Controllers.Clear();

            foreach (var controller in controllersToInclude)
            {
                feature.Controllers.Add(controller.GetTypeInfo());
            }
        }
    }
}