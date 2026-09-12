// 
//  Copyright 2013 AnyUnit Contributors
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnyUnit.Run;
using AnyUnit.Run.Attributes;
using AnyUnit.Util;

namespace AnyUnit.Style.Nunit
{

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TestAttribute:Run.Attributes.TestAttributeBase
    {

        public override TestParameterSetProducer ParameterSets
        {
            get
            {
                return method =>
                           {

                               var list = new List<ParameterSet>();

                               var cases= method
                                   .GetCustomAttributes(typeof(TestCaseAttribute), true)
                                   .OfType<TestCaseAttribute>().ToList();
                               if (cases.Any())
                               {
                                   list.AddRange(cases.Select(a => new ParameterSet(a.Arguments)));
                               }

                               // Any other style's row attribute (e.g. xUnit's
                               // InlineDataAttribute) also implementing
                               // IRowInlineParameter - TestCaseAttribute is
                               // excluded here since it's already covered above.
                               var otherRows = method.GetCustomAttributes(true)
                                   .OfType<IRowInlineParameter>()
                                   .Where(a => !(a is TestCaseAttribute))
                                   .ToList();
                               if (otherRows.Any())
                               {
                                   list.AddRange(otherRows.Select(a => new ParameterSet(a.Arguments)));
                               }

                               // Any other style's method-level generating attribute
                               // (e.g. xUnit's ClassDataAttribute/PropertyDataAttribute)
                               // implementing IGeneratingParameter - no NUnit type
                               // implements this today, so nothing to exclude.
                               var generatedRows = method.GetCustomAttributes(true)
                                   .OfType<IGeneratingParameter>()
                                   .SelectMany(g => g.GetData(method, new Type[] { }))
                                   .ToList();
                               if (generatedRows.Any())
                               {
                                   list.AddRange(generatedRows.Select(a => new ParameterSet(a)));
                               }

                               // Per-parameter combinatorial values: NUnit's own
                               // ValuesAttribute/ValueSourceAttribute/RandomAttribute,
                               // or any other style's own IArgParameter, generalized
                               // from the concrete ParameterDataAttribute type so a
                               // future style's own attribute is recognized too.
                               var values = method.GetParameters().Select(p=> new { Prop = p, Attr=
                               p.GetCustomAttributes(true)
                               .OfType<IArgParameter>().FirstOrDefault()
                               }).ToList();

                               if (values.Any() && values.All(v => v.Attr != null))
                               {
                                   var sets = values.Select(v => v.Attr.GetData(v.Prop).Cast<object>().ToList());
                                   var accum = Enumerable.Empty<IEnumerable<Object>>();
                                   accum = sets.Aggregate(accum, CombineHelper);
                                   list.AddRange(accum.Select(v=>new ParameterSet(v.ToArray())));
                               }

                               if (list.Any())
                               {
                                   return list;
                               }
                               return base.ParameterSets(method);
                           };
            }
        }


        private IEnumerable<IEnumerable<Object>> CombineHelper(IEnumerable<IEnumerable<Object>> accum, IEnumerable<Object> sequence)
        {
            var list = new List<IEnumerable<object>>();

            var first = !accum.Any();
            foreach (var item in sequence)
            {
                if (first)
                {
                    list.Add(new[]{item});
                }
                else
                {
                    list.AddRange(accum.Select(more => more.Concat(new[] {item})));
                }
            }
            return list;
        }


  

        public override TestInvoker TestInvoke
        {
            get
            {
                return (helper, method, target, args) =>
                           {

                               var ignore =method.GetCustomAttributes(typeof (IgnoreAttribute), true)
                                     .OfType<IgnoreAttribute>()
                                     .FirstOrDefault();

                               if (ignore != null)
                               {
                                   throw new IgnoreException(ignore.Reason);
                               }

                               var platform = method.GetCustomAttributes(typeof (PlatformAttribute), true)
                                     .OfType<PlatformAttribute>()
                                     .FirstOrDefault();

                               string platformReason;
                               if (platform != null && !platform.IsSupported(out platformReason))
                               {
                                   throw new IgnoreException(platformReason);
                               }

                               var setUpMethod = GetMethodForAttribute(target, typeof (SetUpAttribute));
                               var teardownMethod = GetMethodForAttribute(target, typeof (TearDownAttribute));

                               // Class-level actions wrap method-level ones: BeforeTest runs
                               // class-level first, AfterTest runs method-level first.
                               var targetType = target as Type ?? target.GetType();
                               var testActions = targetType.GetCustomAttributes(true).OfType<ITestAction>()
                                   .Concat(method.GetCustomAttributes(true).OfType<ITestAction>())
                                   .ToList();

                               TestCycleExceptions te = null;
                               Func<TestCycleExceptions> exceptions = () => te ?? (te = new TestCycleExceptions());
                               try //TryCatch Setup Errors
                               {
                                   foreach (var action in testActions)
                                       action.BeforeTest(method);

                                   if (setUpMethod != null)
                                       setUpMethod.Invoke(target, null);
                                   try //TryCatch Test Errors
                                   {
                                       return base.TestInvoke(helper, method, target, args);
                                   }
                                   catch (Exception ex)
                                   {
                                       exceptions().Add(TestCycle.Test, ex);
                                   }
                               }
                               catch (Exception ex)
                               {
                                  exceptions().Add(TestCycle.Setup, ex);
                               }
                               finally
                               {
                                   try //TryCatch Teardown Errors
                                   {
                                       if (teardownMethod != null)
                                           teardownMethod.Invoke(target, null);
                                   }
                                   catch (Exception ex)
                                   {
                                       exceptions().Add(TestCycle.Teardown, ex);
                                   }

                                   try //TryCatch Test Action Errors
                                   {
                                       foreach (var action in Enumerable.Reverse(testActions))
                                           action.AfterTest(method);
                                   }
                                   catch (Exception ex)
                                   {
                                       exceptions().Add(TestCycle.Teardown, ex);
                                   }

                                   //If any errors occur throw them to next level
                                   if (te != null)
                                   {
                                       throw exceptions();
                                   }
                               }
                               throw new Exception("This point should never be reached");
                           };
            }
        }


        protected MethodInfo GetMethodForAttribute(object fixture, Type attributeType)
        {
            var type = fixture as Type ?? fixture.GetType();
            return type.GetFlattenedMethods(includeNonPublic:true)
                .FirstOrDefault(m => m.GetCustomAttributes(attributeType, true).Any());
        }
     

        public override int GetTimeout(MethodInfo method)
        {
            return method.GetCustomAttributes(typeof(TimeoutAttribute), true)
                                        .OfType<TimeoutAttribute>()
                                        .Select(trait => (int?)trait.Timeout)
                                        .FirstOrDefault().GetValueOrDefault(-1);
        }

        public override IList<string> GetCategories(MethodInfo method)
        {
            return (method.GetCustomAttributes(typeof(CategoryAttribute), true)
                      .OfType<CategoryAttribute>()
                      .Select(trait => trait.Name)).ToList();
        }

        public override string GetDescription(MethodInfo method)
        {
            return Description ?? method.GetCustomAttributes(typeof (DescriptionAttribute), true)
                                        .OfType<DescriptionAttribute>()
                                        .Select(trait => trait.Description)
                                        .FirstOrDefault();
        }

        public string Description { get; set; }
    }
}