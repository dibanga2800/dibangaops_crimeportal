# Image Optimization Guide

This guide provides best practices for optimizing images in your application for better performance.

## Current Images

The following image files are currently in the `public` directory:

- `A1logo.jpg` - Company logo (JPEG)
- `A1logo1.png` - Company logo alternate (PNG)
- `central coop.png` - Central Co-op logo (PNG)
- `coop_bg_img.png` - Background image (PNG)
- `favicon.ico` - Favicon

## Optimization Steps

### 1. Compress Images

Use image compression tools to reduce file sizes without significant quality loss:

#### Online Tools (Recommended)
- **TinyPNG** - https://tinypng.com/ (PNG/JPEG)
- **Squoosh** - https://squoosh.app/ (All formats, by Google)
- **ImageOptim** - https://imageoptim.com/ (Mac)
- **Compressor.io** - https://compressor.io/ (All formats)

#### CLI Tools
```bash
# Install Sharp CLI for batch optimization
npm install -g sharp-cli

# Optimize PNG files
sharp -i "*.png" -o compressed/ --compressionLevel 9

# Optimize JPEG files
sharp -i "*.jpg" -o compressed/ --quality 80
```

#### NPM Scripts (Automated)
Add these scripts to your `package.json`:

```json
{
  "scripts": {
    "optimize-images": "node scripts/optimize-images.js"
  }
}
```

Create `scripts/optimize-images.js`:
```javascript
// Image optimization script using Sharp
import sharp from 'sharp'
import fs from 'fs'
import path from 'path'

const publicDir = path.join(process.cwd(), 'public')
const outputDir = path.join(process.cwd(), 'public-optimized')

// Ensure output directory exists
if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir)
}

// Get all image files
const images = fs.readdirSync(publicDir).filter(file => 
  /\.(jpg|jpeg|png)$/i.test(file)
)

// Optimize each image
images.forEach(async (image) => {
  const inputPath = path.join(publicDir, image)
  const ext = path.extname(image).toLowerCase()
  const baseName = path.basename(image, ext)
  
  if (ext === '.png') {
    // Optimize PNG
    await sharp(inputPath)
      .png({ quality: 80, compressionLevel: 9 })
      .toFile(path.join(outputDir, image))
  } else if (ext === '.jpg' || ext === '.jpeg') {
    // Optimize JPEG
    await sharp(inputPath)
      .jpeg({ quality: 80, progressive: true })
      .toFile(path.join(outputDir, image))
  }
  
  console.log(`✓ Optimized ${image}`)
})
```

### 2. Convert to WebP

WebP provides better compression than PNG/JPEG. Modern browsers support it.

```bash
# Install Sharp for conversion
npm install -g sharp-cli

# Convert to WebP
sharp -i "*.png" -o webp/ -f webp --quality 80
sharp -i "*.jpg" -o webp/ -f webp --quality 80
```

Then use WebP with fallback in your code:
```html
<picture>
  <source srcset="/logo.webp" type="image/webp">
  <img src="/logo.png" alt="Logo">
</picture>
```

### 3. Use Appropriate Formats

- **Logos & Icons**: Use SVG when possible (scalable, small file size)
- **Photos**: Use JPEG or WebP
- **Graphics with transparency**: Use PNG or WebP
- **Favicons**: Keep as ICO or use SVG

### 4. Responsive Images

Provide different image sizes for different screen sizes:

```html
<img
  srcset="
    /logo-small.png 480w,
    /logo-medium.png 768w,
    /logo-large.png 1200w
  "
  sizes="(max-width: 600px) 480px, (max-width: 900px) 768px, 1200px"
  src="/logo-medium.png"
  alt="Logo"
/>
```

### 5. Lazy Loading

Enable lazy loading for images below the fold:

```jsx
// React component
<img 
  src="/image.jpg" 
  alt="Description"
  loading="lazy"
/>
```

### 6. Image Optimization Checklist

For each image, verify:

- [ ] Compressed to appropriate quality level
- [ ] Correct format (SVG > WebP > JPEG/PNG)
- [ ] Proper dimensions (not larger than displayed)
- [ ] Lazy loading enabled (if below fold)
- [ ] Responsive images provided (if needed)
- [ ] Alt text provided for accessibility
- [ ] No unused images in project

## Recommended Image Sizes

### Logos
- **Header Logo**: 200x50px (max)
- **Favicon**: 32x32px, 16x16px
- **App Icon**: 512x512px

### Background Images
- **Desktop**: 1920x1080px (max)
- **Tablet**: 1024x768px
- **Mobile**: 750x1334px

### Content Images
- **Thumbnails**: 300x200px
- **Medium**: 800x600px
- **Large**: 1200x900px

## File Size Targets

Aim for these file sizes after optimization:

- **Logos**: < 20KB
- **Icons**: < 10KB
- **Background Images**: < 200KB
- **Content Images**: < 100KB each
- **Thumbnails**: < 30KB

## Automation with Vite

Vite can automatically optimize images during build. Install plugin:

```bash
npm install -D vite-plugin-imagemin
```

Add to `vite.config.ts`:

```typescript
import viteImagemin from 'vite-plugin-imagemin'

export default defineConfig({
  plugins: [
    viteImagemin({
      gifsicle: { optimizationLevel: 3 },
      optipng: { optimizationLevel: 7 },
      mozjpeg: { quality: 80 },
      pngquant: { quality: [0.8, 0.9], speed: 4 },
      svgo: {
        plugins: [
          { name: 'removeViewBox', active: false },
          { name: 'removeEmptyAttrs', active: false }
        ]
      }
    })
  ]
})
```

## CDN & Image Services

For production, consider using an image CDN:

- **Cloudinary** - Automatic optimization & transformations
- **Imgix** - Real-time image processing
- **AWS CloudFront** - Fast global delivery
- **Vercel Image Optimization** - Built-in with Vercel hosting

Example with Cloudinary:
```jsx
<img 
  src="https://res.cloudinary.com/demo/image/upload/w_400,f_auto,q_auto/sample.jpg"
  alt="Optimized image"
/>
```

## Monitoring Image Performance

Use these tools to monitor image performance:

1. **Chrome DevTools**
   - Network tab → Filter by images
   - Check file sizes and load times

2. **Lighthouse**
   - Run audit in Chrome DevTools
   - Check "Properly size images" and "Efficiently encode images"

3. **WebPageTest**
   - https://www.webpagetest.org/
   - Detailed waterfall analysis

## Quick Win Recommendations

Based on your current images:

1. **Background Image (`coop_bg_img.png`)**
   - Likely largest file
   - Consider converting to JPEG (if no transparency needed)
   - Or convert to WebP
   - Compress to 70-80% quality

2. **Logos (`A1logo.jpg`, `A1logo1.png`, `central coop.png`)**
   - If simple logos, convert to SVG for best quality/size
   - Otherwise, compress PNG logos
   - Use WebP format with PNG fallback

3. **Remove Unused Images**
   - If you have both `A1logo.jpg` and `A1logo1.png`, keep only one
   - Remove any other unused images

## Resources

- [web.dev Image Optimization Guide](https://web.dev/fast/#optimize-your-images)
- [MDN Responsive Images](https://developer.mozilla.org/en-US/docs/Learn/HTML/Multimedia_and_embedding/Responsive_images)
- [Sharp Documentation](https://sharp.pixelplumbing.com/)
- [WebP Browser Support](https://caniuse.com/webp)

---

**Note**: Image optimization should be done before deployment to production. Consider adding it to your build pipeline for automatic optimization.
