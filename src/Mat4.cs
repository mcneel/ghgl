using Rhino.Geometry;

namespace ghgl
{
    struct Mat4
    {
        internal float _00, _01, _02, _03;
        internal float _10, _11, _12, _13;
        internal float _20, _21, _22, _23;
        internal float _30, _31, _32, _33;

        public Mat4(Transform xform)
        {
            _00 = (float)xform.M00;
            _01 = (float)xform.M01;
            _02 = (float)xform.M02;
            _03 = (float)xform.M03;

            _10 = (float)xform.M10;
            _11 = (float)xform.M11;
            _12 = (float)xform.M12;
            _13 = (float)xform.M13;

            _20 = (float)xform.M20;
            _21 = (float)xform.M21;
            _22 = (float)xform.M22;
            _23 = (float)xform.M23;

            _30 = (float)xform.M30;
            _31 = (float)xform.M31;
            _32 = (float)xform.M32;
            _33 = (float)xform.M33;
        }

        public override string ToString()
        {
            return $"{_00},{_01},{_02},{_03},{_10},{_11},{_12},{_13},{_20},{_21},{_22},{_23},{_30},{_31},{_32},{_33}";
        }
    }
}
