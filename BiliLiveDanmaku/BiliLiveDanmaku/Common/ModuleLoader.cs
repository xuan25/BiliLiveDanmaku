using BiliLiveDanmaku.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BiliLiveDanmaku.Common
{
    class ModuleLoader
    {
        public List<IModule> LoadModules()
        {
            List<IModule> processWrappers = CreateInterface<IModule>(Assembly.GetExecutingAssembly());
            DirectoryInfo pluginsDirectoryInfo = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "plugins"));
            if (pluginsDirectoryInfo.Exists)
            {
                FileInfo[] fileInfos = pluginsDirectoryInfo.GetFiles();
                foreach (FileInfo fileInfo in fileInfos)
                {
                    if (fileInfo.Extension.ToLower() == ".afpt-plugin")
                    {
                        Console.WriteLine("[{0}] [Info] Loading plugin: {1}", GetType().Name, fileInfo.Name);
                        List<IModule> processWrappersBuffer = CreateInterface<IModule>(fileInfo.FullName);
                        processWrappers.AddRange(processWrappersBuffer);
                    }
                }
            }

            return processWrappers;
        }

        private List<T> CreateInterface<T>(string dllpath)
        {
            Assembly assembly = Assembly.LoadFrom(dllpath);
            return CreateInterface<T>(assembly);
        }


        private List<T> CreateInterface<T>(Assembly assembly)
        {
            List<T> instances = new List<T>();
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsClass && type.IsPublic && type.IsSealed && !type.IsAbstract && typeof(T).IsAssignableFrom(type))
                {
                    T instance = (T)assembly.CreateInstance(type.FullName);
                    instances.Add(instance);
                }
            }
            return instances;
        }
    }
}
