using LitMotion;
using LitMotion.Extensions;
using Naninovel;
using Spine.Unity;
using UnityEngine;

[CommandAlias("emote")]
public class NaninovelCharacterEmotionEffect : Command
{
    public StringParameter name;
    public StringParameter target;
    public DecimalParameter time;
    public StringParameter pos;

    public override UniTask Execute(AsyncToken token = default)
    {
        if (Assigned(name) && Assigned(target))
        {
            // find character
            var obj = GameObject.Find($"Naninovel<Runtime>/Character/{target}/{target}");
            if (obj)
            {
                var ani = obj.GetComponent<SkeletonAnimation>();
                if (ani)
                {
                    string position = !Assigned(pos) ? "Front" : pos;

                    var res = GameObject.Instantiate(Resources.Load(name), ani.transform) as GameObject;
                    res.transform.localScale = Vector3.one;
                    res.transform.localPosition = new Vector3(1.7f * (position == "Front" ? 1 : -1), 12.17f, 0);

                    var renderer = res.GetComponent<SpriteRenderer>();
                    if (renderer)
                        LMotion.Create(0f, renderer.color.a, 0.2f)
                            .BindToColorA(renderer)
                            .AddTo(res);

                    GameObject.Destroy(res, time.Value);
                }
            }
        }

        return UniTask.CompletedTask;
    }
}