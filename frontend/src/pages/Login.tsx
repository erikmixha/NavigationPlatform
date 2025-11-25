import { useEffect } from 'react';

const Login = () => {
  useEffect(() => {
    window.location.href = '/api/auth/login';
  }, []);

  return (
    <div
      style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}
    >
      <div>Redirecting to login...</div>
    </div>
  );
};

export default Login;
