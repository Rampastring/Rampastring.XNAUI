namespace Rampastring.XNAUI;

using Microsoft.Xna.Framework.Graphics;

public readonly struct SpriteBatchSettings
{
    public SpriteBatchSettings(SpriteSortMode ssm, BlendState bs, SamplerState ss, DepthStencilState dss, RasterizerState rs, Effect effect)
    {
        SpriteSortMode = ssm;
        BlendState = bs;
        SamplerState = ss;
        DepthStencilState = dss;
        RasterizerState = rs;
        Effect = effect;
    }

    public readonly SpriteSortMode SpriteSortMode;
    public readonly SamplerState SamplerState;
    public readonly BlendState BlendState;
    public readonly DepthStencilState DepthStencilState;
    public readonly RasterizerState RasterizerState;
    public readonly Effect Effect;
}