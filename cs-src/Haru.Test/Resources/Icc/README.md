# Test ICC profiles

From [Compact-ICC-Profiles](https://github.com/saucecontrol/Compact-ICC-Profiles) by Clinton Ingram, CC0 1.0
Universal (public domain), downloaded on 2026-11-27 from `profiles/` of the `master` branch:

- `ProPhoto-v2-micro.icc` - a wide-gamut RGB profile (ICC v2): colors in it render visibly different from sRGB
- `DisplayP3-v4.icc` - an RGB profile of ICC version 4
- `sGrey-v2-micro.icc` - a gray profile (ICC v2)
- `prophoto.jpg` - an 8 x 4 JPEG made with Pillow that embeds `ProPhoto-v2-micro.icc` (APP2 ICC_PROFILE)
