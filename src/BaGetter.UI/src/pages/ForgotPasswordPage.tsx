import { Link } from "react-router";
import { Button } from "@/components/ui/button";

export function ForgotPasswordPage() {
  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold">Forgot Password</h2>
      <p className="text-sm text-[var(--fg-muted)]">
        Please contact your administrator to reset your password.
      </p>
      <Link to="/account/login" className="mt-4 block">
        <Button variant="outline" className="w-full">
          Back to Sign In
        </Button>
      </Link>
    </div>
  );
}
