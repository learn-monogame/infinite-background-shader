using System;
using Apos.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameProject {
    public class Game1 : Game {
        public Game1() {
            _graphics = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            Content.RootDirectory = "Content";
            Window.AllowUserResizing = true;
        }

        protected override void Initialize() {
            base.Initialize();
        }

        protected override void LoadContent() {
            _s = new SpriteBatch(GraphicsDevice);

            InputHelper.Setup(this);

            _background = Content.Load<Texture2D>("background");
            _infinite = Content.Load<Effect>("infinite");

            _targetScale = _maxScale;
            _scale = _targetScale;
        }

        protected override void Update(GameTime gameTime) {
            InputHelper.UpdateSetup();

            if (_quit.Pressed())
                Exit();

            var width = GraphicsDevice.Viewport.Width;
            var height = GraphicsDevice.Viewport.Height;

            _origin = new Vector2(width / 2f, height / 2f);

            UpdateCameraInput();

            _scale = InterpolateTowardsTarget(_scale, _targetScale, _speed, _snapDistance);
            _rotation = InterpolateTowardsTarget(_rotation, _targetRotation, _speed, _snapDistance);

            InputHelper.UpdateCleanup();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            float width = GraphicsDevice.Viewport.Width;
            float height = GraphicsDevice.Viewport.Height;

            Matrix projection = Matrix.CreateOrthographicOffCenter(-1, 1, 1, -1, 0, 1);
            Matrix uv_transform = GetUVTransform(_background, new Vector2(0, 0), 1f, GraphicsDevice.Viewport);

            _infinite.Parameters["view_projection"].SetValue(Matrix.Identity * projection);
            _infinite.Parameters["uv_transform"].SetValue(Matrix.Invert(uv_transform));

            _s.Begin(effect: _infinite, samplerState: SamplerState.LinearWrap);
            _s.Draw(_background, new Rectangle(-1, -1, 2, 2), Color.White);
            _s.End();

            base.Draw(gameTime);
        }

        private void UpdateCameraInput() {
            int scrollDelta = MouseCondition.ScrollDelta;
            if (scrollDelta != 0) {
                _targetScale = MathHelper.Clamp(
                    LogDistanceToScale(ScaleToLogDistance(_targetScale) - scrollDelta * _scrollToLogDistance)
                    , _minScale, _maxScale);
            }

            if (RotateLeft.Pressed()) {
                _targetRotation += MathHelper.Pi / 8f;
            }
            if (RotateRight.Pressed()) {
                _targetRotation -= MathHelper.Pi / 8f;
            }

            _mouseWorld = Vector2.Transform(InputHelper.NewMouse.Position.ToVector2(), Matrix.Invert(GetView()));

            if (CameraDrag.Pressed()) {
                _dragAnchor = _mouseWorld;
                _isDragged = true;
            }
            if (_isDragged && CameraDrag.HeldOnly()) {
                _xy += _dragAnchor - _mouseWorld;
                _mouseWorld = _dragAnchor;
            }
            if (_isDragged && CameraDrag.Released()) {
                _isDragged = false;
            }
        }

        /// <summary>
        /// Poor man's tweening function.
        /// If the result is stored in the `from` value, it will create a nice interpolation over multiple frames.
        /// </summary>
        /// <param name="from">The value to start from.</param>
        /// <param name="target">The value to reach.</param>
        /// <param name="speed">A value between 0f and 1f.</param>
        /// <param name="snapNear">
        /// When the difference between the target and the result is smaller than this value, the target will be returned.
        /// </param>
        /// <returns></returns>
        private float InterpolateTowardsTarget(float from, float target, float speed, float snapNear) {
            float result = MathHelper.Lerp(from, target, speed);

            if (from < target) {
                result = MathHelper.Clamp(result, from, target);
            } else {
                result = MathHelper.Clamp(result, target, from);
            }

            if (MathF.Abs(target - result) < snapNear) {
                return target;
            } else {
                return result;
            }
        }

        private Matrix GetView() {
            return
                Matrix.CreateTranslation(-_origin.X, -_origin.Y, 0f) *
                Matrix.CreateTranslation(-_xy.X, -_xy.Y, 0f) *
                Matrix.CreateRotationZ(_rotation) *
                Matrix.CreateScale(_scale, _scale, 1f) *
                Matrix.CreateTranslation(_origin.X, _origin.Y, 0f);
        }
        private Matrix GetUVTransform(Texture2D t, Vector2 offset, float scale, Viewport v) {
            return
                Matrix.CreateScale(_background.Width, _background.Height, 1f) *
                Matrix.CreateScale(scale, scale, 1f) *
                Matrix.CreateTranslation(offset.X, offset.Y, 0f) *
                GetView() *
                Matrix.CreateScale(1f / v.Width, 1f / v.Height, 1f);
        }

        private float ScaleToLogDistance(float scale) {
            return MathF.Log(1f / scale + 1f);
        }
        private float LogDistanceToScale(float value) {
            return 1f / (MathF.Exp(value) - 1f);
        }

        GraphicsDeviceManager _graphics;
        SpriteBatch _s;

        Texture2D _background;
        Effect _infinite;

        Vector2 _origin;
        Vector2 _xy = new Vector2(0f, 0f);
        float _scale;
        float _rotation = 0f;

        float _targetScale;
        float _targetRotation = 0f;
        float _speed = 0.1f;
        float _snapDistance = 0.001f;

        Vector2 _mouseWorld = Vector2.Zero;
        Vector2 _dragAnchor = Vector2.Zero;
        bool _isDragged = false;

        float _scrollToLogDistance = 0.001f;
        float _minScale = 0.1f;
        float _maxScale = 1f;

        ICondition _quit =
            new AnyCondition(
                new KeyboardCondition(Keys.Escape),
                new GamePadCondition(GamePadButton.Back, 0)
            );

        ICondition RotateLeft = new KeyboardCondition(Keys.OemComma);
        ICondition RotateRight = new KeyboardCondition(Keys.OemPeriod);

        ICondition CameraDrag = new MouseCondition(MouseButton.MiddleButton);
    }
}
