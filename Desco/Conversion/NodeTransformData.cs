using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Cobalt.IO;

using Desco.ModelParser;

namespace Desco.Conversion
{
    // TODO: meehh

    public class NodeTransformData
    {
        public TransformData TranslationX { get; private set; }
        public TransformData TranslationY { get; private set; }
        public TransformData TranslationZ { get; private set; }
        public TransformData RotationX { get; private set; }
        public TransformData RotationY { get; private set; }
        public TransformData RotationZ { get; private set; }
        public TransformData ScaleX { get; private set; }
        public TransformData ScaleY { get; private set; }
        public TransformData ScaleZ { get; private set; }

        public NodeTransformData(
            TransformData translationX, TransformData translationY, TransformData translationZ,
            TransformData rotationX, TransformData rotationY, TransformData rotationZ,
            TransformData scaleX, TransformData scaleY, TransformData scaleZ)
        {
            TranslationX = translationX;
            TranslationY = translationY;
            TranslationZ = translationZ;

            RotationX = rotationX;
            RotationY = rotationY;
            RotationZ = rotationZ;

            ScaleX = scaleX;
            ScaleY = scaleY;
            ScaleZ = scaleZ;
        }
    }
}
