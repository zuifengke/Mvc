﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageInvokerProviderTest
    {
        [Fact]
        public void GetOrAddCacheEntry_PopulatesCacheEntry()
        {
            // Arrange
            var descriptor = new PageActionDescriptor
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
            };
            Func<PageContext, object> factory = _ => null;
            Action<PageContext, object> releaser = (_, __) => { };
            var loader = new Mock<IPageLoader>();
            loader.Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(typeof(object));
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.Setup(p => p.ActionDescriptors).Returns(descriptorCollection);
            var factoryProvider = new Mock<IPageFactoryProvider>();
            factoryProvider.Setup(f => f.CreatePageFactory(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(factory);
            factoryProvider.Setup(f => f.CreatePageDisposer(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(releaser);

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                actionDescriptorProvider.Object,
                factoryProvider.Object);
            var context = new ActionInvokerProviderContext(new ActionContext
            {
                ActionDescriptor = descriptor,
            });

            // Act
            var entry = invokerProvider.GetOrAddCacheEntry(context, descriptor);

            // Assert
            Assert.NotNull(entry);
            var compiledPageActionDescriptor = Assert.IsType<CompiledPageActionDescriptor>(entry.ActionDescriptor);
            Assert.Equal(descriptor.RelativePath, compiledPageActionDescriptor.RelativePath);
            Assert.Same(factory, entry.PageFactory);
            Assert.Same(releaser, entry.ReleasePage);
        }

        [Fact]
        public void GetOrAddCacheEntry_CachesEntries()
        {
            // Arrange
            var descriptor = new PageActionDescriptor
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
            };
            var loader = new Mock<IPageLoader>();
            loader.Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(typeof(object));
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.Setup(p => p.ActionDescriptors).Returns(descriptorCollection);

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                actionDescriptorProvider.Object,
                Mock.Of<IPageFactoryProvider>());
            var context = new ActionInvokerProviderContext(new ActionContext
            {
                ActionDescriptor = descriptor,
            });

            // Act
            var entry1 = invokerProvider.GetOrAddCacheEntry(context, descriptor);
            var entry2 = invokerProvider.GetOrAddCacheEntry(context, descriptor);

            // Assert
            Assert.Same(entry1, entry2);
        }

        [Fact]
        public void GetOrAddCacheEntry_UpdatesEntriesWhenActionDescriptorProviderCollectionIsUpdated()
        {
            // Arrange
            var descriptor = new PageActionDescriptor
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
            };
            var descriptorCollection1 = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var descriptorCollection2 = new ActionDescriptorCollection(new[] { descriptor }, version: 2);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.SetupSequence(p => p.ActionDescriptors)
                .Returns(descriptorCollection1)
                .Returns(descriptorCollection2);

            var loader = new Mock<IPageLoader>();
            loader.Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(typeof(object));
            var invokerProvider = CreateInvokerProvider(
                 loader.Object,
                 actionDescriptorProvider.Object,
                 Mock.Of<IPageFactoryProvider>());
            var context = new ActionInvokerProviderContext(new ActionContext
            {
                ActionDescriptor = descriptor,
            });

            // Act
            var entry1 = invokerProvider.GetOrAddCacheEntry(context, descriptor);
            var entry2 = invokerProvider.GetOrAddCacheEntry(context, descriptor);

            // Assert
            Assert.NotSame(entry1, entry2);
        }

        private static PageActionInvokerProvider CreateInvokerProvider(
            IPageLoader loader,
            IActionDescriptorCollectionProvider actionDescriptorProvider,
            IPageFactoryProvider factoryProvider)
        {
            return new PageActionInvokerProvider(
                loader,
                factoryProvider,
                actionDescriptorProvider,
                new IFilterProvider[0],
                new IValueProviderFactory[0],
                new EmptyModelMetadataProvider(),
                Mock.Of<ITempDataDictionaryFactory>(),
                new TestOptions<HtmlHelperOptions>(),
                Mock.Of<IPageHandlerMethodSelector>(),
                new DiagnosticListener("Microsoft.AspNetCore"),
                NullLoggerFactory.Instance);
        }
    }
}
