﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace IgorO.ExposedObjectProject
{
    public class ExposedObject : DynamicObject
    {
        private object m_object;
        private Type m_type;
        private Dictionary<string, Dictionary<int, List<MethodInfo>>> m_instanceMethods;
        private Dictionary<string, Dictionary<int, List<MethodInfo>>> m_genInstanceMethods;

        private ExposedObject(object obj)
        {
            m_object = obj;
            m_type = obj.GetType();

            m_instanceMethods =
                m_type
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => !m.IsGenericMethod)
                    .GroupBy(m => m.Name)
                    .ToDictionary(
                        p => p.Key,
                        p => p.GroupBy(r => r.GetParameters().Length).ToDictionary(r => r.Key, r => r.ToList()));

            m_genInstanceMethods =
                m_type
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.IsGenericMethod)
                    .GroupBy(m => m.Name)
                    .ToDictionary(
                        p => p.Key,
                        p => p.GroupBy(r => r.GetParameters().Length).ToDictionary(r => r.Key, r => r.ToList()));
        }

        public object Object { get { return m_object; } }

        public static dynamic New<T>()
        {
            return New(typeof(T));
        }

        public static dynamic New(Type type)
        {
            return new ExposedObject(Create(type));
        }

        private static object Create(Type type)
        {
            ConstructorInfo constructorInfo = GetConstructorInfo(type);
            return constructorInfo.Invoke(new object[0]);
        }

        private static ConstructorInfo GetConstructorInfo(Type type, params Type[] args)
        {
            ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, args, null);
            if (constructorInfo != null)
            {
                return constructorInfo;
            }

            throw new MissingMemberException(type.FullName, string.Format(".ctor({0})", string.Join(", ", Array.ConvertAll(args, t => t.FullName))));
        }

        public static dynamic From(object obj)
        {
            return new ExposedObject(obj);
        }

        public static T Cast<T>(ExposedObject t)
        {
            return (T)t.m_object;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            // Get type args of the call
            Type[] typeArgs = ExposedObjectHelper.GetTypeArgs(binder);
            if (typeArgs != null && typeArgs.Length == 0) typeArgs = null;

            //
            // Try to call a non-generic instance method
            //
            if (typeArgs == null
                    && m_instanceMethods.ContainsKey(binder.Name)
                    && m_instanceMethods[binder.Name].ContainsKey(args.Length)
                    && ExposedObjectHelper.InvokeBestMethod(args, m_object, m_instanceMethods[binder.Name][args.Length], out result))
            {
                return true;
            }

            //
            // Try to call a generic instance method
            //
            if (m_instanceMethods.ContainsKey(binder.Name)
                    && m_instanceMethods[binder.Name].ContainsKey(args.Length))
            {
                List<MethodInfo> methods = new List<MethodInfo>();

                foreach (var method in m_genInstanceMethods[binder.Name][args.Length])
                {
                    if (method.GetGenericArguments().Length == typeArgs.Length)
                    {
                        methods.Add(method.MakeGenericMethod(typeArgs));
                    }
                }

                if (ExposedObjectHelper.InvokeBestMethod(args, m_object, methods, out result))
                {
                    return true;
                }
            }

            result = null;
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var propertyInfo = m_type.GetProperty(
                binder.Name,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo != null)
            {
                propertyInfo.SetValue(m_object, value, null);
                return true;
            }

            var fieldInfo = m_type.GetField(
                binder.Name,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            if (fieldInfo != null)
            {
                fieldInfo.SetValue(m_object, value);
                return true;
            }

            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var propertyInfo = m_object.GetType().GetProperty(
                binder.Name,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo != null)
            {
                result = propertyInfo.GetValue(m_object, null);
                return true;
            }

            var fieldInfo = m_object.GetType().GetField(
                binder.Name,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            if (fieldInfo != null)
            {
                result = fieldInfo.GetValue(m_object);
                return true;
            }

            result = null;
            return false;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            result = m_object;
            return true;
        }
    }

}


