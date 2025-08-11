namespace Stride.CommunityToolkit.Mathematics;

/// <summary>
/// A collection of easing functions.
/// </summary>
public static partial class Easing
{
    /// <summary>
    /// Performs easing using the specified function.
    /// </summary>
    /// <param name="amount">The amount.</param>
    /// <param name="function">The easing function to use.</param>
    /// <returns>The amount eased using the specified function.</returns>
    public static float Ease(float amount, EasingFunction function)
    {
        return function switch
        {
            EasingFunction.QuadraticEaseOut => QuadraticEaseOut(amount),
            EasingFunction.QuadraticEaseIn => QuadraticEaseIn(amount),
            EasingFunction.QuadraticEaseInOut => QuadraticEaseInOut(amount),
            EasingFunction.CubicEaseIn => CubicEaseIn(amount),
            EasingFunction.CubicEaseOut => CubicEaseOut(amount),
            EasingFunction.CubicEaseInOut => CubicEaseInOut(amount),
            EasingFunction.QuarticEaseIn => QuarticEaseIn(amount),
            EasingFunction.QuarticEaseOut => QuarticEaseOut(amount),
            EasingFunction.QuarticEaseInOut => QuarticEaseInOut(amount),
            EasingFunction.QuinticEaseIn => QuinticEaseIn(amount),
            EasingFunction.QuinticEaseOut => QuinticEaseOut(amount),
            EasingFunction.QuinticEaseInOut => QuinticEaseInOut(amount),
            EasingFunction.SineEaseIn => SineEaseIn(amount),
            EasingFunction.SineEaseOut => SineEaseOut(amount),
            EasingFunction.SineEaseInOut => SineEaseInOut(amount),
            EasingFunction.CircularEaseIn => CircularEaseIn(amount),
            EasingFunction.CircularEaseOut => CircularEaseOut(amount),
            EasingFunction.CircularEaseInOut => CircularEaseInOut(amount),
            EasingFunction.ExponentialEaseIn => ExponentialEaseIn(amount),
            EasingFunction.ExponentialEaseOut => ExponentialEaseOut(amount),
            EasingFunction.ExponentialEaseInOut => ExponentialEaseInOut(amount),
            EasingFunction.ElasticEaseIn => ElasticEaseIn(amount),
            EasingFunction.ElasticEaseOut => ElasticEaseOut(amount),
            EasingFunction.ElasticEaseInOut => ElasticEaseInOut(amount),
            EasingFunction.BackEaseIn => BackEaseIn(amount),
            EasingFunction.BackEaseOut => BackEaseOut(amount),
            EasingFunction.BackEaseInOut => BackEaseInOut(amount),
            EasingFunction.BounceEaseIn => BounceEaseIn(amount),
            EasingFunction.BounceEaseOut => BounceEaseOut(amount),
            EasingFunction.BounceEaseInOut => BounceEaseInOut(amount),
            _ => Linear(amount),
        };
    }

    /// <summary>
    /// Performs a linear easing.
    /// </summary>
    /// <param name="amount">The amount.</param>
    /// <returns>The amount eased.</returns>
    /// <remarks>
    /// Modeled after the line y = x
    /// </remarks>
    public static float Linear(float amount)
    {
        return amount;
    }

    /// <remarks>
    /// Modeled after the parabola y = x^2
    /// </remarks>
    public static float QuadraticEaseIn(float amount)
    {
        return amount * amount;
    }

    /// <remarks>
    /// Modeled after the parabola y = -x^2 + 2x
    /// </remarks>
    public static float QuadraticEaseOut(float amount)
    {
        return -(amount * (amount - 2));
    }

    /// <remarks>
    /// Modeled after the piecewise quadratic
    /// y = (1/2)((2x)^2)             ; [0, 0.5]
    /// y = -(1/2)((2x-1)*(2x-3) - 1) ; [0.5, 1]
    /// </remarks>
    public static float QuadraticEaseInOut(float amount)
    {
        if (amount < 0.5f)
        {
            return 2 * amount * amount;
        }
        else
        {
            return (-2 * amount * amount) + (4 * amount) - 1;
        }
    }

    /// <remarks>
    /// Modeled after the cubic y = x^3
    /// </remarks>
    public static float CubicEaseIn(float amount)
    {
        return amount * amount * amount;
    }

    /// <remarks>
    /// Modeled after the cubic y = (x - 1)^3 + 1
    /// </remarks>
    public static float CubicEaseOut(float amount)
    {
        float f = (amount - 1);
        return f * f * f + 1;
    }

    /// <remarks>
    /// Modeled after the piecewise cubic
    /// y = (1/2)((2x)^3)       ; [0, 0.5]
    /// y = (1/2)((2x-2)^3 + 2) ; [0.5, 1]
    /// </remarks>
    public static float CubicEaseInOut(float amount)
    {
        if (amount < 0.5f)
        {
            return 4 * amount * amount * amount;
        }
        else
        {
            float f = ((2 * amount) - 2);
            return 0.5f * f * f * f + 1;
        }
    }

    /// <remarks>
    /// Modeled after the quartic x^4
    /// </remarks>
    public static float QuarticEaseIn(float amount)
    {
        return amount * amount * amount * amount;
    }

    /// <remarks>
    /// Modeled after the quartic y = 1 - (x - 1)^4
    /// </remarks>
    public static float QuarticEaseOut(float amount)
    {
        float f = (amount - 1);
        return f * f * f * (1 - amount) + 1;
    }

    /// <remarks>
    /// Modeled after the piecewise quartic
    /// y = (1/2)((2x)^4)        ; [0, 0.5]
    /// y = -(1/2)((2x-2)^4 - 2) ; [0.5, 1]
    /// </remarks>
    public static float QuarticEaseInOut(float amount)
    {
        if (amount < 0.5f)
        {
            return 8 * amount * amount * amount * amount;
        }
        else
        {
            float f = (amount - 1);
            return -8 * f * f * f * f + 1;
        }
    }

    /// <remarks>
    /// Modeled after the quintic y = x^5
    /// </remarks>
    public static float QuinticEaseIn(float amount)
    {
        return amount * amount * amount * amount * amount;
    }

    /// <remarks>
    /// Modeled after the quintic y = (x - 1)^5 + 1
    /// </remarks>
    public static float QuinticEaseOut(float amount)
    {
        float f = (amount - 1);
        return f * f * f * f * f + 1;
    }

    /// <remarks>
    /// Modeled after the piecewise quintic
    /// y = (1/2)((2x)^5)       ; [0, 0.5]
    /// y = (1/2)((2x-2)^5 + 2) ; [0.5, 1]
    /// </remarks>
    public static float QuinticEaseInOut(float amount)
    {
        if (amount < 0.5f)
        {
            return 16 * amount * amount * amount * amount * amount;
        }
        else
        {
            float f = ((2 * amount) - 2);
            return 0.5f * f * f * f * f * f + 1;
        }
    }

    /// <remarks>
    /// Modeled after quarter-cycle of sine wave
    /// </remarks>
    public static float SineEaseIn(float amount)
    {
        return (float)Math.Sin((amount - 1) * MathUtil.PiOverTwo) + 1;
    }

    /// <remarks>
    /// Modeled after quarter-cycle of sine wave (different phase)
    /// </remarks>
    public static float SineEaseOut(float amount)
    {
        return (float)Math.Sin(amount * MathUtil.PiOverTwo);
    }

    /// <remarks>
    /// Modeled after half sine wave
    /// </remarks>
    public static float SineEaseInOut(float amount)
    {
        return 0.5f * (1 - (float)Math.Cos(amount * MathUtil.Pi));
    }

    /// <remarks>
    /// Modeled after shifted quadrant IV of unit circle
    /// </remarks>
    public static float CircularEaseIn(float amount)
    {
        return 1 - (float)Math.Sqrt(1 - (amount * amount));
    }

    /// <remarks>
    /// Modeled after shifted quadrant II of unit circle
    /// </remarks>
    public static float CircularEaseOut(float amount)
    {
        return (float)Math.Sqrt((2 - amount) * amount);
    }

    /// <remarks>
    /// Modeled after the piecewise circular function
    /// y = (1/2)(1 - Math.Sqrt(1 - 4x^2))           ; [0, 0.5]
    /// y = (1/2)(Math.Sqrt(-(2x - 3)*(2x - 1)) + 1) ; [0.5, 1]
    /// </remarks>
    public static float CircularEaseInOut(float amount)
    {
        if (amount < 0.5f)
        {
            return 0.5f * (1 - (float)Math.Sqrt(1 - 4 * (amount * amount)));
        }
        else
        {
            return 0.5f * ((float)Math.Sqrt(-((2 * amount) - 3) * ((2 * amount) - 1)) + 1);
        }
    }

    /// <remarks>
    /// Modeled after the exponential function y = 2^(10(x - 1))
    /// </remarks>
    public static float ExponentialEaseIn(float amount)
    {
        return (amount == 0.0f) ? amount : (float)Math.Pow(2, 10 * (amount - 1));
    }

    /// <remarks>
    /// Modeled after the exponential function y = -2^(-10x) + 1
    /// </remarks>
    public static float ExponentialEaseOut(float amount)
    {
        return (amount == 1.0f) ? amount : 1 - (float)Math.Pow(2, -10 * amount);
    }

    /// <remarks>
    /// Modeled after the piecewise exponential
    /// y = (1/2)2^(10(2x - 1))         ; [0,0.5]
    /// y = -(1/2)*2^(-10(2x - 1))) + 1 ; [0.5,1]
    /// </remarks>
    public static float ExponentialEaseInOut(float amount)
    {
        if (amount == 0.0 || amount == 1.0) return amount;

        if (amount < 0.5f)
        {
            return 0.5f * (float)Math.Pow(2, (20 * amount) - 10);
        }
        else
        {
            return -0.5f * (float)Math.Pow(2, (-20 * amount) + 10) + 1;
        }
    }

    /// <remarks>
    /// Modeled after the damped sine wave y = sin(13pi/2*x)*Math.Pow(2, 10 * (x - 1))
    /// </remarks>
    public static float ElasticEaseIn(float amount)
    {
        return (float)Math.Sin(13 * MathUtil.PiOverTwo * amount) * (float)Math.Pow(2, 10 * (amount - 1));
    }

    /// <remarks>
    /// Modeled after the damped sine wave y = sin(-13pi/2*(x + 1))*Math.Pow(2, -10x) + 1
    /// </remarks>
    public static float ElasticEaseOut(float amount)
    {
        return (float)Math.Sin(-13 * MathUtil.PiOverTwo * (amount + 1)) * (float)Math.Pow(2, -10 * amount) + 1;
    }

    /// <remarks>
    /// Modeled after the piecewise exponentially-damped sine wave:
    /// y = (1/2)*sin(13pi/2*(2*x))*Math.Pow(2, 10 * ((2*x) - 1))      ; [0,0.5]
    /// y = (1/2)*(sin(-13pi/2*((2x-1)+1))*Math.Pow(2,-10(2*x-1)) + 2) ; [0.5, 1]
    /// </remarks>
    public static float ElasticEaseInOut(float amount)
    {
        if (amount < 0.5f)
        {
            return 0.5f * (float)Math.Sin(13 * MathUtil.PiOverTwo * (2 * amount)) * (float)Math.Pow(2, 10 * ((2 * amount) - 1));
        }
        else
        {
            return 0.5f * ((float)Math.Sin(-13 * MathUtil.PiOverTwo * ((2 * amount - 1) + 1)) * (float)Math.Pow(2, -10 * (2 * amount - 1)) + 2);
        }
    }

    /// <remarks>
    /// Modeled after the overshooting cubic y = x^3-x*sin(x*pi)
    /// </remarks>
    public static float BackEaseIn(float amount)
    {
        return amount * amount * amount - amount * (float)Math.Sin(amount * MathUtil.Pi);
    }

    /// <remarks>
    /// Modeled after overshooting cubic y = 1-((1-x)^3-(1-x)*sin((1-x)*pi))
    /// </remarks>
    public static float BackEaseOut(float amount)
    {
        float f = (1 - amount);
        return 1 - (f * f * f - f * (float)Math.Sin(f * MathUtil.Pi));
    }

    /// <remarks>
    /// Modeled after the piecewise overshooting cubic function:
    /// y = (1/2)*((2x)^3-(2x)*sin(2*x*pi))           ; [0, 0.5]
    /// y = (1/2)*(1-((1-x)^3-(1-x)*sin((1-x)*pi))+1) ; [0.5, 1]
    /// </remarks>
    public static float BackEaseInOut(float amount)
    {
        if (amount < 0.5f)
        {
            float f = 2 * amount;
            return 0.5f * (f * f * f - f * (float)Math.Sin(f * MathUtil.Pi));
        }
        else
        {
            float f = (1 - (2 * amount - 1));
            return 0.5f * (1 - (f * f * f - f * (float)Math.Sin(f * MathUtil.Pi))) + 0.5f;
        }
    }

    /// <summary>
    /// Produces a bouncing motion that starts slowly, accelerates and ends with bounces (ease-in variant).
    /// </summary>
    /// <param name="amount">Normalized time in range [0,1].</param>
    /// <returns>Interpolated value.</returns>
    public static float BounceEaseIn(float amount)
    {
        return 1 - BounceEaseOut(1 - amount);
    }

    /// <summary>
    /// Produces a bouncing motion that decelerates towards the end (ease-out variant).
    /// </summary>
    /// <param name="amount">Normalized time in range [0,1].</param>
    /// <returns>Interpolated value.</returns>
    public static float BounceEaseOut(float amount)
    {
        if (amount < 4 / 11.0f)
        {
            return (121 * amount * amount) / 16.0f;
        }
        else if (amount < 8 / 11.0f)
        {
            return (363 / 40.0f * amount * amount) - (99 / 10.0f * amount) + 17 / 5.0f;
        }
        else if (amount < 9 / 10.0f)
        {
            return (4356 / 361.0f * amount * amount) - (35442 / 1805.0f * amount) + 16061 / 1805.0f;
        }
        else
        {
            return (54 / 5.0f * amount * amount) - (513 / 25.0f * amount) + 268 / 25.0f;
        }
    }

    /// <summary>
    /// Produces a bouncing motion that eases in during the first half and eases out with bounces in the second half.
    /// </summary>
    /// <param name="amount">Normalized time in range [0,1].</param>
    /// <returns>Interpolated value.</returns>
    public static float BounceEaseInOut(float amount)
    {
        if (amount < 0.5f)
        {
            return 0.5f * BounceEaseIn(amount * 2);
        }
        else
        {
            return 0.5f * BounceEaseOut(amount * 2 - 1) + 0.5f;
        }
    }
}