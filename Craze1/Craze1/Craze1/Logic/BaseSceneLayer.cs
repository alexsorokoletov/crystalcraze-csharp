using Cocos2D;

namespace CC_Test1.Logic
{
    public class BaseSceneLayer<T> : CCLayer
        where T : CCNode, new()
    {
        public static CCScene Scene()
        {
            var scene = new CCScene() { };
            scene.AddChild(new T());
            return scene;
        }
    }
}