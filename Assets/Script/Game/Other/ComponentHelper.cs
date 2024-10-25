using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Script.Game.Item.Coin;
using UnityEngine;

namespace Script.Game.Other
{
    public class ComponentHelper : MonoBehaviour
    {
        // Singleton instance
        public static ComponentHelper Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Tùy chọn: giữ singleton sống giữa các cảnh
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public enum ComponentResult
        {
            Skip, // Bỏ qua các component không cần thiết
            NotFound, // Không tìm thấy thuộc tính hoặc phương thức
            Found // Tìm thấy thuộc tính hoặc phương thức
        }

        // Phương thức trả về giá trị của một trường (field) nếu nó tồn tại trong các component của GameObject
        public object GetFieldValue(GameObject obj, string fieldName, out ComponentResult result)
        {
            Component[] components = obj.GetComponents<Component>();

            foreach (Component component in components)
            {
                System.Type type = component.GetType();

                // Bỏ qua các component cụ thể
                if (type == typeof(BoxCollider2D) || type == typeof(SpriteRenderer) || type == typeof(Transform) ||
                    type == typeof(CoinUser))
                {
                    result = ComponentResult.Skip;
                    continue;
                }

                // Kiểm tra trường (field)
                FieldInfo field = type.GetField(fieldName);
                if (field != null)
                {
                    result = ComponentResult.Found;
                    return field.GetValue(component);
                }
            }

            result = ComponentResult.NotFound;
            return null;
        }

        // Phương thức trả về giá trị của một thuộc tính nếu nó tồn tại trong các component của GameObject
        public object GetPropertyValue(GameObject obj, string propertyName, out ComponentResult result)
        {
            Component[] components = obj.GetComponents<Component>();

            foreach (Component component in components)
            {
                System.Type type = component.GetType();

                // Bỏ qua các component cụ thể
                if (type == typeof(BoxCollider2D) || type == typeof(SpriteRenderer) || type == typeof(Transform) ||
                    type == typeof(CoinUser))
                {
                    result = ComponentResult.Skip;
                    continue;
                }

                // Kiểm tra thuộc tính (property)
                PropertyInfo property = type.GetProperty(propertyName);
                if (property != null)
                {
                    result = ComponentResult.Found;
                    return property.GetValue(component);
                }
            }

            result = ComponentResult.NotFound;
            return null;
        }

        // Phương thức trả về một phương thức (method) nếu nó tồn tại trong các component của GameObject
        public MethodInfo GetMethod(GameObject obj, string methodName, out ComponentResult result)
        {
            Component[] components = obj.GetComponents<Component>();

            foreach (Component component in components)
            {
                System.Type type = component.GetType();

                // Bỏ qua các component cụ thể
                if (type == typeof(BoxCollider2D) || type == typeof(SpriteRenderer) || type == typeof(Transform) ||
                    type == typeof(CoinUser))
                {
                    result = ComponentResult.Skip;
                    continue;
                }

                // Kiểm tra phương thức (method)
                MethodInfo method = type.GetMethod(methodName);
                if (method != null)
                {
                    result = ComponentResult.Found;
                    return method;
                }
            }

            result = ComponentResult.NotFound;
            return null;
        }
    }
}
