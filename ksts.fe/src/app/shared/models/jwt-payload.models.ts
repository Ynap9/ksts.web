export type IJwtPayload = {
    sub: string;
    username?: string;
    name?: string;
    email?: string;
    user_id?: string;
    role?: string | string[];
    iat: number;
    exp: number;
};
